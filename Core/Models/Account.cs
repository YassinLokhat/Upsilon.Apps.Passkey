using System.ComponentModel;
using Upsilon.Apps.PassKey.Core.Enums;
using Upsilon.Apps.PassKey.Core.Interfaces;

namespace Upsilon.Apps.PassKey.Core.Models
{
   internal sealed class Account : IAccount, IChangable
   {
      #region IAccount interface explicit implementation

      string IItem.ItemId => ItemId;
      IService IAccount.Service => Service;

      string IAccount.Label
      {
         get => Label;
         set => Label = Database.AutoSave.UpdateValue(ItemId, nameof(Label), value);
      }

      string[] IAccount.Identifiants
      {
         get => Identifiants;
         set => Identifiants = Database.AutoSave.UpdateValue(ItemId, nameof(Identifiants), value);
      }

      public string Password
      {
         get => Passwords.Count != 0 ? Passwords[Passwords.Keys.Max()] : string.Empty;
         set
         {
            if (!string.IsNullOrEmpty(value))
            {
               Passwords[DateTime.Now] = value;

               if (_service != null)
               {
                  _ = Database.AutoSave.UpdateValue(ItemId, nameof(Passwords), Passwords);
               }
            }
         }
      }

      Dictionary<DateTime, string> IAccount.Passwords => new(Passwords);

      string IAccount.Notes
      {
         get => Notes;
         set => Notes = Database.AutoSave.UpdateValue(ItemId, nameof(Notes), value);
      }

      int IAccount.PasswordUpdateReminderDelay
      {
         get => PasswordUpdateReminderDelay;
         set => PasswordUpdateReminderDelay = Database.AutoSave.UpdateValue(ItemId, nameof(PasswordUpdateReminderDelay), value);
      }

      AccountOption IAccount.Options
      {
         get => Options;
         set => Options = Database.AutoSave.UpdateValue(ItemId, nameof(Options), value);
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
      public string[] Identifiants { get; set; } = [];
      public Dictionary<DateTime, string> Passwords { get; set; } = [];
      public string Notes { get; set; } = string.Empty;
      public int PasswordUpdateReminderDelay { get; set; } = 0;
      public AccountOption Options { get; set; } = AccountOption.None;

      public void Apply(Change change)
      {
         switch (change.ActionType)
         {
            case ChangeType.Update:
               switch (change.FieldName)
               {
                  case nameof(Label):
                     Label = Database.SerializationCenter.Deserialize<string>(change.Value);
                     break;
                  case nameof(Identifiants):
                     Identifiants = Database.SerializationCenter.Deserialize<string[]>(change.Value);
                     break;
                  case nameof(Notes):
                     Notes = Database.SerializationCenter.Deserialize<string>(change.Value);
                     break;
                  case nameof(Passwords):
                     Passwords = Database.SerializationCenter.Deserialize<Dictionary<DateTime, string>>(change.Value);
                     break;
                  case nameof(PasswordUpdateReminderDelay):
                     PasswordUpdateReminderDelay = Database.SerializationCenter.Deserialize<int>(change.Value);
                     break;
                  case nameof(Options):
                     Options = Database.SerializationCenter.Deserialize<AccountOption>(change.Value);
                     break;
                  default:
                     throw new InvalidDataException("FieldName not valid");
               }
               break;
            default:
               throw new InvalidEnumArgumentException(nameof(change.ActionType), (int)change.ActionType, typeof(ChangeType));
         }
      }
   }
}