using System.ComponentModel;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;
using Upsilon.Apps.Passkey.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   /// <summary>
   /// Logged-in vault owner: services, settings, passkeys, RSA private key, and
   /// the inactivity timer. Item id is prefixed <c>U</c>.
   /// </summary>
   internal sealed class User : IUser, IDisposable
   {
      #region IUser interface explicit Internal

      string IItem.ItemId => Host.Touch(ItemId);

      IDatabase IItem.Database => Host.AsDatabase;

      IEnumerable<IService> IUser.Services => [.. Host.Touch(Services)];

      string IUser.Username
      {
         get => Host.Touch(Username);
         set
         {
            CredentialChanged |= Username != value;

            Username = Host.AutoSave.UpdateValue(ItemId,
               fieldName: nameof(Username),
               needsReview: true,
               oldValue: Username,
               newValue: value,
               readableValue: value);
         }
      }

      IEnumerable<string> IUser.Passkeys
      {
         get => Host.Touch<IEnumerable<string>>([.. Passkeys.Select(x => x.Reveal())]);
         set
         {
            IEnumerable<ProtectedSecret> newPasskeys = [.. value.Select(ProtectedSecret.Protect)];

            CredentialChanged |= Host.SerializationCenter.AreDifferent(Passkeys, newPasskeys);

            Passkeys = Host.AutoSave.UpdateValue(ItemId,
               fieldName: nameof(Passkeys),
               needsReview: true,
               oldValue: Passkeys,
               newValue: newPasskeys,
               readableValue: string.Empty);
         }
      }

      ISettings IUser.Settings
      {
         get => Host.Touch(Settings);
         set
         {
            if (value.GetType() != typeof(Settings))
            {
               throw new InvalidCastException("The ISettings object is not a known implementation");
            }

            Settings = Host.Touch((Settings)value);
         }
      }

      IService IUser.AddService(string serviceName)
      {
         Service service = new()
         {
            User = this,
            ItemId = "S" + Host.CryptographyCenter.GetHash(ItemId + serviceName),
            ServiceName = serviceName
         };

         Services.Add(Host.AutoSave.AddValue(ItemId, readableValue: service.ToString(), needsReview: false, value: service));

         return service;
      }

      void IUser.DeleteService(IService service)
      {
         Service serviceToRemove = Services.FirstOrDefault(x => x.ItemId == service.ItemId)
            ?? throw new KeyNotFoundException($"The {service} was not found into the {this}'s services list");

         _ = Services.Remove(Host.AutoSave.DeleteValue(ItemId, readableValue: serviceToRemove.ToString(), needsReview: true, value: serviceToRemove));
      }

      #endregion

      internal IUserHost Host
      {
         get => field ?? throw new NullValueException(nameof(Host));
         set
         {
            field = value;

            Settings.User = this;

            foreach (Service service in Services)
            {
               service.User = this;
            }
         }
      }

      // RSA private key held encrypted in memory and revealed just in time (sign /
      // decrypt / derive public key). Persistence stays a plaintext PEM string inside
      // the onion-encrypted database entry (see ProtectedSecret).
      public ProtectedSecret PrivateKey { get; set; } = ProtectedSecret.Protect(string.Empty);

      // The number of activity-log entries sealed at the last save. Stored inside
      // the encrypted (tamper-proof) database so it can act as a trusted anchor:
      // if the activity log later presents fewer sealed entries, or no signature
      // at all, the log has been rolled back or stripped. Not user-editable, so
      // it deliberately bypasses the AutoSave change-tracking of other fields.
      public int ActivitySealWatermark { get; set; }

      public string ItemId { get; set; } = string.Empty;
      public List<Service> Services { get; set; } = [];

      public string Username { get; set; } = string.Empty;

      // Master passkeys are held encrypted in memory and only revealed just in time
      // (to derive the file keys on save, or to display them in the settings window).
      // Serialization goes through the plaintext (see ProtectedSecret).
      public IEnumerable<ProtectedSecret> Passkeys { get; set; } = [];
      public bool CredentialChanged { get; set; }

      public Settings Settings { get; set; } = new();

      private readonly System.Timers.Timer _timer = new()
      {
         AutoReset = true,
         Enabled = true,
         Interval = 1000,
      };

      public int SessionLeftTime { get; set; }
      private int _clipboardLeftTime;

      // The timer fires on a ThreadPool thread, so a tick can run concurrently
      // with CloseOnSessionTimeout on the UI thread. This gate serializes both: a
      // tick already in progress completes before StopTimer disposes the timer,
      // and a late tick that wins the race observes _timerStopped and bails out
      // before touching the now-disposed vault. The lock is re-entrant, so the
      // timeout tick path (which calls Close -> StopTimer) is safe.
      private readonly System.Threading.Lock _timerGate = new();
      private bool _timerStopped;

      // Clipboard history scrub is async (WinRT). Overlapping ticks must not start
      // a second scrub while one is still enumerating history.
      private int _clipboardScrubRunning;

      public User()
      {
         _timer.Elapsed += _timer_Elapsed;
      }

      private void _timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
      {
         string[]? clipboardScrubList = null;
         IClipboardManager? clipboardManager = null;

         lock (_timerGate)
         {
            if (_timerStopped)
            {
               return;
            }

            if (Settings.LogoutTimeout != 0)
            {
               SessionLeftTime--;

               if (SessionLeftTime == 0)
               {
                  Host.AddActivity(itemId: ItemId,
                     Username,
                     serviceName: null,
                     accountName: null,
                     fieldName: null,
                     fieldValue: null,
                     parentName: null,
                     eventType: ActivityEventType.LoginSessionTimeoutReached,
                     needsReview: true);

                  // Close stops and disposes this timer through StopTimer, so the
                  // tick must not touch the timer or the host afterwards.
                  Host.CloseOnSessionTimeout();

                  return;
               }
            }

            if (Settings.CleaningClipboardTimeout != 0)
            {
               _clipboardLeftTime--;

               if (_clipboardLeftTime == 0)
               {
                  // Capture secrets and the clipboard manager inside the gate, then
                  // scrub outside the lock so we never block the timer on WinRT I/O.
                  clipboardScrubList =
                  [
                     .. Services
                        .SelectMany(x => x.Accounts)
                        .SelectMany(x => x.Passwords.Values.Select(y => y.Reveal())),
                  ];
                  clipboardManager = Host.ClipboardManager;
                  _clipboardLeftTime = Settings.CleaningClipboardTimeout;
               }
            }
         }

         if (clipboardScrubList is not null && clipboardManager is not null)
         {
            _ = _scrubClipboardHistoryAsync(clipboardManager, clipboardScrubList);
         }
      }

      private async Task _scrubClipboardHistoryAsync(IClipboardManager clipboardManager, IEnumerable<string> removeList)
      {
         if (Interlocked.CompareExchange(ref _clipboardScrubRunning, 1, 0) != 0)
         {
            return;
         }

         try
         {
            _ = await clipboardManager.RemoveAllOccurrenceAsync(removeList).ConfigureAwait(false);
         }
         catch (OperationCanceledException ex)
         {
            System.Diagnostics.Trace.TraceWarning($"Clipboard history scrub failed: {ex}");
         }
         finally
         {
            _ = Interlocked.Exchange(ref _clipboardScrubRunning, 0);
         }
      }

      /// <summary>
      /// Restarts the inactivity and clipboard-clean countdowns from the current
      /// settings. Called on login and on every vault field read via
      /// <see cref="IUserHost.Touch{T}"/>.
      /// </summary>
      public void ResetTimer()
      {
         SessionLeftTime = Settings.LogoutTimeout * 60;
         _clipboardLeftTime = Settings.CleaningClipboardTimeout;
      }

      internal void StopTimer()
      {
         lock (_timerGate)
         {
            if (_timerStopped)
            {
               return;
            }

            _timerStopped = true;
            _timer.Stop();
            _timer.Elapsed -= _timer_Elapsed;
            _timer.Dispose();
         }
      }

      /// <summary>
      /// Replays an autosave change. The first character of <see cref="Change.ItemId"/>
      /// is the type prefix: <c>U</c> user, <c>S</c> service, <c>A</c> account.
      /// </summary>
      internal void Apply(Change change)
      {
         switch (change.ItemId[0])
         {
            case 'U':
               _apply(change);
               break;
            case 'S':
               Service service = Services.FirstOrDefault(x => change.ItemId == x.ItemId)
                  ?? throw new KeyNotFoundException($"The Service '{change.ItemId}' was not found into the {this}'s services list");

               service.Apply(change);
               break;
            case 'A':
               Account account = Services.SelectMany(x => x.Accounts).FirstOrDefault(x => change.ItemId == x.ItemId)
                  ?? throw new KeyNotFoundException($"The Account {change.ItemId}' was not found into the {this}'s accounts list");

               account.Apply(change);
               break;
            default:
               throw new InvalidDataException("ItemId not valid");
         }
      }

      private void _apply(Change change)
      {
         switch (change.ActionType)
         {
            case ActivityEventType.ItemUpdated:
               switch (change.FieldName)
               {
                  case nameof(Username):
                     CredentialChanged = true;
                     Username = change.NewValue.DeserializeTo<string>(Host.SerializationCenter);
                     break;
                  case nameof(Passkeys):
                     CredentialChanged = true;
                     Passkeys = change.NewValue.DeserializeTo<IEnumerable<ProtectedSecret>>(Host.SerializationCenter);
                     break;
                  case nameof(Settings.LogoutTimeout):
                     Settings.LogoutTimeout = change.NewValue.DeserializeTo<int>(Host.SerializationCenter);
                     break;
                  case nameof(Settings.CleaningClipboardTimeout):
                     Settings.CleaningClipboardTimeout = change.NewValue.DeserializeTo<int>(Host.SerializationCenter);
                     break;
                  case nameof(Settings.ShowPasswordDelay):
                     Settings.ShowPasswordDelay = change.NewValue.DeserializeTo<int>(Host.SerializationCenter);
                     break;
                  case nameof(Settings.NumberOfOldPasswordToKeep):
                     Settings.NumberOfOldPasswordToKeep = change.NewValue.DeserializeTo<int>(Host.SerializationCenter);
                     break;
                  case nameof(Settings.NumberOfMonthActivitiesToKeep):
                     Settings.NumberOfMonthActivitiesToKeep = change.NewValue.DeserializeTo<int>(Host.SerializationCenter);
                     break;
                  case nameof(Settings.WarningsToNotify):
                     Settings.WarningsToNotify = change.NewValue.DeserializeTo<WarningType>(Host.SerializationCenter);
                     break;
                  case nameof(Settings.Language):
                     Settings.Language = change.NewValue.DeserializeTo<string>(Host.SerializationCenter);
                     break;
                  case nameof(Settings.Theme):
                     Settings.Theme = change.NewValue.DeserializeTo<string>(Host.SerializationCenter);
                     break;
                  default:
                     throw new InvalidDataException("FieldName not valid");
               }
               break;
            case ActivityEventType.ItemAdded:
               Service serviceToAdd = change.NewValue.DeserializeTo<Service>(Host.SerializationCenter);
               serviceToAdd.User = this;
               Services.Add(serviceToAdd);
               break;
            case ActivityEventType.ItemDeleted:
               Service serviceToDelete = change.NewValue.DeserializeTo<Service>(Host.SerializationCenter);
               _ = Services.RemoveAll(x => x.ItemId == serviceToDelete.ItemId);
               break;
            default:
               throw new InvalidEnumArgumentException(nameof(change.ActionType), (int)change.ActionType, typeof(ActivityEventType));
         }
      }

      public override string ToString() => Host.Username;

      public bool HasChanged() => Host.HasPendingChanges(ItemId) || Services.Any(x => x.HasChanged());

      public void Dispose() => _timer?.Dispose();
   }
}
