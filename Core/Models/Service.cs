using System.ComponentModel;
using Upsilon.Apps.Passkey.Core.Interfaces;
using Upsilon.Apps.PassKey.Core.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   internal sealed class Service : IService
   {
      #region IService interface explicit implementation

      string IItem.ItemId => ItemId;
      IUser IService.User => User;
      IEnumerable<IAccount> IService.Accounts => Accounts.Cast<IAccount>();

      string IService.ServiceName
      {
         get => ServiceName;
         set => ServiceName = User.Database.AutoSave.UpdateValue(ItemId, nameof(ServiceName), value);
      }

      string IService.Url
      {
         get => Url;
         set => Url = User.Database.AutoSave.UpdateValue(ItemId, nameof(Url), value);
      }

      string IService.Notes
      {
         get => Notes;
         set => Notes = User.Database.AutoSave.UpdateValue(ItemId, nameof(Notes), value);
      }

      void IService.AddAccount(string label, IEnumerable<string> identifiants, string password)
      {
         Account account = new()
         {
            Service = this,
            ItemId = ItemId + (label + string.Join(string.Empty, identifiants)).GetHash(),
            Label = label,
            Identifiants = identifiants.ToArray(),
            Password = password,
         };

         Accounts.Add(User.Database.AutoSave.AddValue(ItemId, account));
      }

      void IService.DeleteAccount(string accountId)
      {
         Account account = Accounts.FirstOrDefault(x => x.ItemId == accountId)
            ?? throw new KeyNotFoundException($"The '{accountId}' account was not found into the '{ItemId}' service");

         Accounts.Remove(User.Database.AutoSave.DeleteValue(ItemId, account));
      }

      #endregion

      public string ItemId { get; set; } = string.Empty;

      private User? _user;
      public User User
      {
         get => _user ?? throw new NullReferenceException(nameof(User));
         set => _user = value;
      }

      public List<Account> Accounts { get; set; } = new();

      public string ServiceName { get; set; } = string.Empty;
      public string Url { get; set; } = string.Empty;
      public string Notes { get; set; } = string.Empty;

      public void Apply(Change change)
      {
         switch (change.ItemId.Length / SecurityCenter.HashLength)
         {
            case 2:
               _apply(change);
               break;
            case 3:
               Account account = Accounts.FirstOrDefault(x => change.ItemId.StartsWith(x.ItemId))
                  ?? throw new KeyNotFoundException($"The '{change.ItemId}' account was not found into the '{ItemId}' service");

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
            case ChangeType.Update:
               switch (change.FieldName)
               {
                  case nameof(ServiceName):
                     ServiceName = change.Value.Deserialize<string>();
                     break;
                  case nameof(Url):
                     Url = change.Value.Deserialize<string>();
                     break;
                  case nameof(Notes):
                     Notes = change.Value.Deserialize<string>();
                     break;
                  default:
                     throw new InvalidDataException("FieldName not valid");
               }
               break;
            case ChangeType.Add:
               Account accountToAdd = change.Value.Deserialize<Account>();
               Accounts.Add(accountToAdd);
               break;
            case ChangeType.Delete:
               Account accountToDelete = change.Value.Deserialize<Account>();
               Accounts.RemoveAll(x => x.ItemId == accountToDelete.ItemId);
               break;
            default:
               throw new InvalidEnumArgumentException(nameof(change.ActionType), (int)change.ActionType, typeof(ChangeType));
         }
      }
   }
}