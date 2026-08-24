using System.ComponentModel;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;
using Upsilon.Apps.Passkey.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   /// <summary>
   /// Account stored under a <see cref="Service"/>. Explicit interface members
   /// go through <see cref="IUserHost.Touch{T}"/> so a read resets the session
   /// timer and through autosave so a write is persisted after a short debounce.
   /// </summary>
   internal sealed class Account : IAccount
   {
      #region IAccount interface explicit Internal

      string IItem.ItemId => Host.Touch(ItemId);

      IDatabase IItem.Database => Host.AsDatabase;

      IService IAccount.Service => Host.Touch(Service);

      string IAccount.Label
      {
         get => Host.Touch(Label);
         set => Label = Host.AutoSave.UpdateValue(ItemId,
            fieldName: nameof(Label),
            needsReview: false,
            oldValue: Label,
            newValue: value,
            readableValue: value);
      }

      IEnumerable<string> IAccount.Identifiers
      {
         get => Host.Touch(Identifiers);
         set => Identifiers = Host.AutoSave.UpdateValue(ItemId,
            fieldName: nameof(Identifiers),
            needsReview: true,
            oldValue: Identifiers,
            newValue: value,
            readableValue: $"({string.Join(", ", value)})");
      }

      string IAccount.Password
      {
         get => Host.Touch(Password);
         set
         {
            if (!string.IsNullOrEmpty(value)
               && Password != value)
            {
               Dictionary<DateTime, ProtectedSecret> oldPasswords = Passwords.CloneWith(Host.SerializationCenter);
               Password = value;
               Passwords[DateTime.Now] = ProtectedSecret.Protect(value);

               if (_service is not null)
               {
                  if (Service.User.Settings.NumberOfOldPasswordToKeep != 0)
                  {
                     DateTime[] datesToRemove = [.. Passwords.Keys
                        .OrderBy(x => x)
                        .Take(Passwords.Count > Service.User.Settings.NumberOfOldPasswordToKeep
                           ? Passwords.Count - Service.User.Settings.NumberOfOldPasswordToKeep
                           : 0)];

                     foreach (DateTime dateToRemove in datesToRemove)
                     {
                        _ = Passwords.Remove(dateToRemove);
                     }
                  }

                  _ = Host.AutoSave.UpdateValue(ItemId,
                     fieldName: nameof(Password),
                     needsReview: true,
                     oldValue: oldPasswords,
                     newValue: Passwords,
                     readableValue: string.Empty);
               }
            }
         }
      }

      Dictionary<DateTime, string> IAccount.Passwords => Passwords.ToDictionary(x => x.Key, x => x.Value.Reveal());

      string IAccount.Notes
      {
         get => Host.Touch(Notes);
         set => Notes = Host.AutoSave.UpdateValue(ItemId,
            fieldName: nameof(Notes),
            needsReview: false,
            oldValue: Notes,
            newValue: value,
            readableValue: value);
      }

      int IAccount.PasswordUpdateReminderDelay
      {
         get => Host.Touch(PasswordUpdateReminderDelay);
         set => PasswordUpdateReminderDelay = Host.AutoSave.UpdateValue(ItemId,
            fieldName: nameof(PasswordUpdateReminderDelay),
            needsReview: false,
            oldValue: PasswordUpdateReminderDelay,
            newValue: value,
            readableValue: $"{value}");
      }

      AccountOption IAccount.Options
      {
         get => Host.Touch(Options);
         set => Options = Host.AutoSave.UpdateValue(ItemId,
            fieldName: nameof(Options),
            needsReview: false,
            oldValue: Options,
            newValue: value,
            readableValue: value.ToString());
      }

      #endregion

      internal IUserHost Host => Service.User.Host;

      public string ItemId { get; set; } = string.Empty;

      private Service? _service;
      internal Service Service
      {
         get => _service ?? throw new NullValueException(nameof(Service));
         set => _service = value;
      }

      public string Label { get; set; } = string.Empty;
      public IEnumerable<string> Identifiers { get; set; } = [];

      // Backed by a ProtectedSecret so the plaintext is never held in a long-lived
      // field: it is only materialized just in time on read and re-protected on
      // write. Serialization goes through the plaintext (see ProtectedSecret), so
      // the persisted form is unchanged.
      public string Password
      {
         get => _password.Reveal();
         set => _password = ProtectedSecret.Protect(value);
      }
      private ProtectedSecret _password = ProtectedSecret.Protect(string.Empty);

      public Dictionary<DateTime, ProtectedSecret> Passwords { get; set; } = [];
      public string Notes { get; set; } = string.Empty;
      public int PasswordUpdateReminderDelay { get; set; }
      public AccountOption Options { get; set; }
         = AccountOption.WarnIfPasswordLeaked;

      internal bool PasswordExpired
      {
         get
         {
            // No reminder configured, or no dated history yet (e.g. a bad import
            // path that set Password without seeding Passwords): treat as fresh.
            if (PasswordUpdateReminderDelay == 0
               || Passwords.Count == 0)
            {
               return false;
            }

            DateTime lastPassword = Passwords.Keys.Max();
            int delay = ((DateTime.Now.Year - lastPassword.Year) * 12) + DateTime.Now.Month - lastPassword.Month;

            return delay > PasswordUpdateReminderDelay;
         }
      }

      internal bool PasswordLeaked { get; set; }

      internal void Apply(Change change)
      {
         switch (change.ActionType)
         {
            case ActivityEventType.ItemUpdated:
               switch (change.FieldName)
               {
                  case nameof(Label):
                     Label = change.NewValue.DeserializeTo<string>(Host.SerializationCenter);
                     break;
                  case nameof(Identifiers):
                     Identifiers = change.NewValue.DeserializeTo<string[]>(Host.SerializationCenter);
                     break;
                  case nameof(Notes):
                     Notes = change.NewValue.DeserializeTo<string>(Host.SerializationCenter);
                     break;
                  case nameof(Password):
                     Passwords = change.NewValue.DeserializeTo<Dictionary<DateTime, ProtectedSecret>>(Host.SerializationCenter);
                     Password = Passwords.Count != 0 ? Passwords[Passwords.Keys.Max()].Reveal() : string.Empty;
                     break;
                  case nameof(PasswordUpdateReminderDelay):
                     PasswordUpdateReminderDelay = change.NewValue.DeserializeTo<int>(Host.SerializationCenter);
                     break;
                  case nameof(Options):
                     Options = change.NewValue.DeserializeTo<AccountOption>(Host.SerializationCenter);
                     break;
                  default:
                     throw new InvalidDataException("FieldName not valid");
               }
               break;
            default:
               throw new InvalidEnumArgumentException(nameof(change.ActionType), (int)change.ActionType, typeof(ActivityEventType));
         }
      }

      public override string ToString()
      {
         string account = "Account ";

         if (!string.IsNullOrEmpty(Label))
         {
            account += $"{Label} ";
         }

         return account + $"({string.Join(", ", Identifiers)})";
      }

      public bool HasChanged() => Host.HasPendingChanges(ItemId);
   }
}
