using System.ComponentModel;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.Core.Models
{
   internal sealed class User : IUser
   {
      #region IUser interface explicit Internal

      string IItem.ItemId => Database.Get(ItemId);

      IDatabase IItem.Database => Database;

      IService[] IUser.Services => [.. Database.Get(Services)];

      string IUser.Username
      {
         get => Database.Get(Username);
         set
         {
            CredentialChanged |= Username != value;

            Username = Database.AutoSave.UpdateValue(ItemId,
               itemName: ToString(),
               fieldName: nameof(Username),
               needsReview: true,
               oldValue: Username,
               newValue: value,
               readableValue: value);
         }
      }

      string[] IUser.Passkeys
      {
         get => Database.Get(Passkeys);
         set
         {
            CredentialChanged |= Database.SerializationCenter.AreDifferent(Passkeys, value);

            Passkeys = Database.AutoSave.UpdateValue(ItemId,
               itemName: ToString(),
               fieldName: nameof(Passkeys),
               needsReview: true,
               oldValue: Passkeys,
               newValue: value,
               readableValue: string.Empty);
         }
      }

      int IUser.LogoutTimeout
      {
         get => Database.Get(LogoutTimeout);
         set => LogoutTimeout = Database.AutoSave.UpdateValue(ItemId,
            itemName: ToString(),
            fieldName: nameof(LogoutTimeout),
            needsReview: false,
            oldValue: LogoutTimeout,
            newValue: value,
            readableValue: value.ToString());
      }

      int IUser.CleaningClipboardTimeout
      {
         get => Database.Get(CleaningClipboardTimeout);
         set => CleaningClipboardTimeout = Database.AutoSave.UpdateValue(ItemId,
            itemName: ToString(),
            fieldName: nameof(CleaningClipboardTimeout),
            needsReview: false,
            oldValue: CleaningClipboardTimeout,
            newValue: value,
            readableValue: value.ToString());
      }

      int IUser.ShowPasswordDelay
      {
         get => Database.Get(ShowPasswordDelay);
         set => ShowPasswordDelay = Database.AutoSave.UpdateValue(ItemId,
            itemName: ToString(),
            fieldName: nameof(ShowPasswordDelay),
            needsReview: false,
            oldValue: ShowPasswordDelay,
            newValue: value,
            readableValue: value.ToString());
      }

      int IUser.NumberOfOldPasswordToKeep
      {
         get => Database.Get(NumberOfOldPasswordToKeep);
         set
         {
            NumberOfOldPasswordToKeep = Database.AutoSave.UpdateValue(ItemId,
               itemName: ToString(),
               fieldName: nameof(NumberOfOldPasswordToKeep),
               needsReview: true,
               oldValue: NumberOfOldPasswordToKeep,
               newValue: value,
               readableValue: value.ToString());

            if (NumberOfOldPasswordToKeep == 0) return;

            Account[] accounts = [.. Services.SelectMany(x => x.Accounts).Where(x => x.Passwords.Count > NumberOfOldPasswordToKeep)];

            foreach (Account account in accounts)
            {
               DateTime[] datesToRemove = [.. account.Passwords.Keys
                  .OrderBy(x => x)
                  .Take(account.Passwords.Count - NumberOfOldPasswordToKeep)];

               foreach (DateTime dateToRemove in datesToRemove)
               {
                  _ = account.Passwords.Remove(dateToRemove);
               }
            }
         }
      }

      WarningType IUser.WarningsToNotify
      {
         get => Database.Get(WarningsToNotify);
         set => WarningsToNotify = Database.AutoSave.UpdateValue(ItemId,
            itemName: ToString(),
            fieldName: nameof(WarningsToNotify),
            needsReview: true,
            oldValue: WarningsToNotify,
            newValue: value,
            readableValue: value.ToString());
      }

      IService IUser.AddService(string serviceName)
      {
         Service service = new()
         {
            User = this,
            ItemId = ItemId + Database.CryptographyCenter.GetHash(serviceName),
            ServiceName = serviceName
         };

         Services.Add(Database.AutoSave.AddValue(ItemId, itemName: service.ToString(), containerName: ToString(), needsReview: false, value: service));

         return service;
      }

      void IUser.DeleteService(IService service)
      {
         Service serviceToRemove = Services.FirstOrDefault(x => x.ItemId == service.ItemId)
            ?? throw new KeyNotFoundException($"The '{service.ItemId}' service was not found into the '{ItemId}' user");

         _ = Services.Remove(Database.AutoSave.DeleteValue(ItemId, itemName: serviceToRemove.ToString(), containerName: ToString(), needsReview: true, value: serviceToRemove));
      }

      #endregion

      internal Database Database
      {
         get => field ?? throw new NullReferenceException(nameof(Database));
         set
         {
            field = value;

            foreach (Service service in Services)
            {
               service.User = this;
            }
         }
      }

      public string PrivateKey { get; set; } = string.Empty;

      public string ItemId { get; set; } = string.Empty;
      public List<Service> Services { get; set; } = [];

      public string Username { get; set; } = string.Empty;
      public string[] Passkeys { get; set; } = [];
      public bool CredentialChanged { get; set; } = false;
      public int LogoutTimeout { get; set; } = 0;
      public int CleaningClipboardTimeout { get; set; } = 0;
      public int ShowPasswordDelay { get; set; } = 0;
      public int NumberOfOldPasswordToKeep { get; set; } = 0;
      public WarningType WarningsToNotify { get; set; }
         = WarningType.LogReviewWarning
         | WarningType.PasswordUpdateReminderWarning
         | WarningType.DuplicatedPasswordsWarning
         | WarningType.PasswordLeakedWarning;

      private readonly System.Timers.Timer _timer = new()
      {
         AutoReset = true,
         Enabled = true,
         Interval = 1000,
      };

      public int SessionLeftTime = 0;
      private int _clipboardLeftTime = 0;

      public User()
      {
         _timer.Elapsed += _timer_Elapsed;
      }

      private void _timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
      {
         if (LogoutTimeout != 0)
         {
            SessionLeftTime--;

            if (SessionLeftTime == 0)
            {
               Database.Logs.AddLog($"User {Username}'s login session timeout reached", needsReview: true);
               Database.Close(logCloseEvent: true, loginTimeoutReached: true);

               _timer.Stop();
               _timer.Dispose();
            }
         }

         if (CleaningClipboardTimeout != 0)
         {
            _clipboardLeftTime--;

            if (_clipboardLeftTime == 0)
            {
               _cleanClipboard();

               _clipboardLeftTime = CleaningClipboardTimeout;
            }
         }
      }

      private void _cleanClipboard()
      {
         string[] passwords = [.. Services.SelectMany(x => x.Accounts).SelectMany(x => x.Passwords.Values)];

         int cleanedPasswordsCount = Database.ClipboardManager.RemoveAllOccurence(passwords);

         if (cleanedPasswordsCount != 0)
         {
            Database.Logs.AddLog($"{cleanedPasswordsCount} passwords was cleaned from User {Username}'s clipboard", needsReview: false);
         }
      }

      public void ResetTimer()
      {
         SessionLeftTime = LogoutTimeout * 60;
         _clipboardLeftTime = CleaningClipboardTimeout;
      }

      public void Apply(Change change)
      {
         switch (change.ItemId.Length / Database.CryptographyCenter.HashLength)
         {
            case 1:
               _apply(change);
               break;
            case 2:
            case 3:
               Service service = Services.FirstOrDefault(x => change.ItemId.StartsWith(x.ItemId))
                  ?? throw new KeyNotFoundException($"The '{change.ItemId[..(2 * Database.CryptographyCenter.HashLength)]}' service was not found into the '{ItemId}' user");

               service.Apply(change);
               break;
            default:
               throw new InvalidDataException("ItemId not valid");
         }
      }

      private void _apply(Change change)
      {
         switch (change.ActionType)
         {
            case Change.Type.Update:
               switch (change.FieldName)
               {
                  case nameof(Username):
                     CredentialChanged = true;
                     Username = change.NewValue.DeserializeTo<string>(Database.SerializationCenter);
                     break;
                  case nameof(Passkeys):
                     CredentialChanged = true;
                     Passkeys = change.NewValue.DeserializeTo<string[]>(Database.SerializationCenter);
                     break;
                  case nameof(LogoutTimeout):
                     LogoutTimeout = change.NewValue.DeserializeTo<int>(Database.SerializationCenter);
                     break;
                  case nameof(CleaningClipboardTimeout):
                     CleaningClipboardTimeout = change.NewValue.DeserializeTo<int>(Database.SerializationCenter);
                     break;
                  case nameof(WarningsToNotify):
                     WarningsToNotify = change.NewValue.DeserializeTo<WarningType>(Database.SerializationCenter);
                     break;
                  default:
                     throw new InvalidDataException("FieldName not valid");
               }
               break;
            case Change.Type.Add:
               Service serviceToAdd = change.NewValue.DeserializeTo<Service>(Database.SerializationCenter);
               serviceToAdd.User = this;
               Services.Add(serviceToAdd);
               break;
            case Change.Type.Delete:
               Service serviceToDelete = change.NewValue.DeserializeTo<Service>(Database.SerializationCenter);
               _ = Services.RemoveAll(x => x.ItemId == serviceToDelete.ItemId);
               break;
            default:
               throw new InvalidEnumArgumentException(nameof(change.ActionType), (int)change.ActionType, typeof(Change.Type));
         }
      }

      public override string ToString() => $"User {Database.Username}";
   }
}