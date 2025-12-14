using System.ComponentModel;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Core.Models
{
   internal sealed class Account : IAccount
   {
      #region IAccount interface explicit Internal

      string IItem.ItemId => Database.Get(ItemId);

      IDatabase IItem.Database => Database;

      IService IAccount.Service => Database.Get(Service);

      string IAccount.Label
      {
         get => Database.Get(Label);
         set => Label = Database.AutoSave.UpdateValue(ItemId,
            itemName: ToString(),
            fieldName: nameof(Label),
            needsReview: false,
            oldValue: Label,
            newValue: value,
            readableValue: value);
      }

      string[] IAccount.Identifiers
      {
         get => Database.Get(Identifiers);
         set => Identifiers = Database.AutoSave.UpdateValue(ItemId,
            itemName: ToString(),
            fieldName: nameof(Identifiers),
            needsReview: true,
            oldValue: Identifiers,
            newValue: value,
            readableValue: $"({string.Join(", ", value)})");
      }

      string IAccount.Password
      {
         get => Database.Get(Password);
         set
         {
            if (!string.IsNullOrEmpty(value)
               && Password != value)
            {
               Dictionary<DateTime, string> oldPasswords = Passwords.CloneWith(Database.SerializationCenter);
               Passwords[DateTime.Now] = Password = value;

               if (_service is not null)
               {
                  if (Service.User.NumberOfOldPasswordToKeep != 0)
                  {
                     DateTime[] datesToRemove = [.. Passwords.Keys
                        .OrderBy(x => x)
                        .Take(Passwords.Count > Service.User.NumberOfOldPasswordToKeep
                           ? Passwords.Count - Service.User.NumberOfOldPasswordToKeep
                           : 0)];

                     foreach (DateTime dateToRemove in datesToRemove)
                     {
                        _ = Passwords.Remove(dateToRemove);
                     }
                  }

                  _ = Database.AutoSave.UpdateValue(ItemId,
                     itemName: ToString(),
                     fieldName: nameof(Password),
                     needsReview: true,
                     oldValue: oldPasswords,
                     newValue: Passwords,
                     readableValue: string.Empty);
               }
            }
         }
      }

      Dictionary<DateTime, string> IAccount.Passwords => new(Passwords);

      string IAccount.Notes
      {
         get => Database.Get(Notes);
         set => Notes = Database.AutoSave.UpdateValue(ItemId,
            itemName: ToString(),
            fieldName: nameof(Notes),
            needsReview: false,
            oldValue: Notes,
            newValue: value,
            readableValue: value);
      }

      int IAccount.PasswordUpdateReminderDelay
      {
         get => Database.Get(PasswordUpdateReminderDelay);
         set => PasswordUpdateReminderDelay = Database.AutoSave.UpdateValue(ItemId,
            itemName: ToString(),
            fieldName: nameof(PasswordUpdateReminderDelay),
            needsReview: false,
            oldValue: PasswordUpdateReminderDelay,
            newValue: value,
            readableValue: value.ToString());
      }

      AccountOption IAccount.Options
      {
         get => Database.Get(Options);
         set => Options = Database.AutoSave.UpdateValue(ItemId,
            itemName: ToString(),
            fieldName: nameof(Options),
            needsReview: false,
            oldValue: Options,
            newValue: value,
            readableValue: value.ToString());
      }

      #endregion

      internal Database Database => Service.User.Database;

      public string ItemId { get; set; } = string.Empty;

      private Service? _service;
      internal Service Service
      {
         get => _service ?? throw new NullReferenceException(nameof(Service));
         set => _service = value;
      }

      public string Label { get; set; } = string.Empty;
      public string[] Identifiers { get; set; } = [];
      public string Password { get; set; } = string.Empty;
      public Dictionary<DateTime, string> Passwords { get; set; } = [];
      public string Notes { get; set; } = string.Empty;
      public int PasswordUpdateReminderDelay { get; set; } = 0;
      public AccountOption Options { get; set; }
         = AccountOption.WarnIfPasswordLeaked;

      internal bool PasswordExpired
      {
         get
         {
            if (PasswordUpdateReminderDelay == 0) return false;

            DateTime lastPassword = Passwords.Keys.Max();
            int delay = ((DateTime.Now.Year - lastPassword.Year) * 12) + DateTime.Now.Month - lastPassword.Month;

            return delay >= PasswordUpdateReminderDelay;
         }
      }

      internal bool PasswordLeaked => Options.ContainsFlag(AccountOption.WarnIfPasswordLeaked) && Database.PasswordFactory.PasswordLeaked(Password);

      public void Apply(Change change)
      {
         switch (change.ActionType)
         {
            case Change.Type.Update:
               switch (change.FieldName)
               {
                  case nameof(Label):
                     Label = change.NewValue.DeserializeTo<string>(Database.SerializationCenter);
                     break;
                  case nameof(Identifiers):
                     Identifiers = change.NewValue.DeserializeTo<string[]>(Database.SerializationCenter);
                     break;
                  case nameof(Notes):
                     Notes = change.NewValue.DeserializeTo<string>(Database.SerializationCenter);
                     break;
                  case nameof(Password):
                     Passwords = change.NewValue.DeserializeTo<Dictionary<DateTime, string>>(Database.SerializationCenter);
                     Password = Passwords.Count != 0 ? Passwords[Passwords.Keys.Max()] : string.Empty;
                     break;
                  case nameof(PasswordUpdateReminderDelay):
                     PasswordUpdateReminderDelay = change.NewValue.DeserializeTo<int>(Database.SerializationCenter);
                     break;
                  case nameof(Options):
                     Options = change.NewValue.DeserializeTo<AccountOption>(Database.SerializationCenter);
                     break;
                  default:
                     throw new InvalidDataException("FieldName not valid");
               }
               break;
            default:
               throw new InvalidEnumArgumentException(nameof(change.ActionType), (int)change.ActionType, typeof(Change.Type));
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
   }
}