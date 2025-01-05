using System.ComponentModel;
using Upsilon.Apps.Passkey.Core.Interfaces;
using Upsilon.Apps.PassKey.Core.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   internal sealed class Account : IAccount
   {
      #region IAccount interface explicit implementation

      string IItem.ItemId => ItemId;
      IService IAccount.Service => Service;

      string IAccount.Label
      {
         get => Label;
         set => Label = Service.User.Database.AutoSave.UpdateValue(ItemId, nameof(Label), value);
      }

      string[] IAccount.Identifiants
      {
         get => Identifiants;
         set => Identifiants = Service.User.Database.AutoSave.UpdateValue(ItemId, nameof(Identifiants), value);
      }

      public string Password
      {
         get => Passwords[Passwords.Keys.Max()];
         set
         {
            Passwords[DateTime.Now.Ticks] = value;
            _ = Service.User.Database.AutoSave.UpdateValue(ItemId, nameof(Passwords), Passwords);
         }
      }

      Dictionary<long, string> IAccount.Passwords => new(Passwords);

      string IAccount.Notes
      {
         get => Notes;
         set => Notes = Service.User.Database.AutoSave.UpdateValue(ItemId, nameof(Notes), value);
      }

      AccountOption IAccount.Options
      {
         get => Options;
         set => Options = Service.User.Database.AutoSave.UpdateValue(ItemId, nameof(Options), value);
      }

      #endregion

      public string ItemId { get; set; } = string.Empty;

      private Service? _service;
      public Service Service
      {
         get => _service ?? throw new NullReferenceException(nameof(Service));
         set => _service = value;
      }

      public string Label { get; set; } = string.Empty;
      public string[] Identifiants { get; set; } = Array.Empty<string>();
      public Dictionary<long, string> Passwords { get; set; } = new();
      public string Notes { get; set; } = string.Empty;
      public AccountOption Options { get; set; }

      public void Apply(Change change)
      {
         switch (change.ActionType)
         {
            case ChangeType.Update:
               switch (change.FieldName)
               {
                  case nameof(Label):
                     Label = change.Value.Deserialize<string>();
                     break;
                  case nameof(Identifiants):
                     Identifiants = change.Value.Deserialize<string[]>();
                     break;
                  case nameof(Notes):
                     Notes = change.Value.Deserialize<string>();
                     break;
                  case nameof(Passwords):
                     Passwords = change.Value.Deserialize<Dictionary<long, string>>();
                     break;
                  case nameof(Options):
                     Options = change.Value.Deserialize<AccountOption>();
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