using System.ComponentModel;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces;

namespace Upsilon.Apps.Passkey.Core.Models
{
   internal sealed class Service : IService
   {
      #region IService interface explicit Internal

      string IItem.ItemId => Database.Get(ItemId);

      IDatabase IItem.Database => Database;

      IUser IService.User => Database.Get(User);
      IAccount[] IService.Accounts => [.. Database.Get(Accounts)];

      string IService.ServiceName
      {
         get => Database.Get(ServiceName);
         set => ServiceName = Database.AutoSave.UpdateValue(ItemId,
            itemName: ToString(),
            fieldName: nameof(ServiceName),
            needsReview: true,
            oldValue: ServiceName,
            newValue: value,
            readableValue: value);
      }

      string IService.Url
      {
         get => Database.Get(Url);
         set => Url = Database.AutoSave.UpdateValue(ItemId,
            itemName: ToString(),
            fieldName: nameof(Url),
            needsReview: false,
            oldValue: Url,
            newValue: value,
            readableValue: value);
      }

      string IService.Notes
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

      public IAccount AddAccount(string label, IEnumerable<string> identifiers, string password)
      {
         Account account = new()
         {
            Service = this,
            ItemId = ItemId + Database.CryptographyCenter.GetHash(label + string.Join(string.Empty, identifiers)),
            Label = label,
            Identifiers = [.. identifiers],
            Password = password,
         };

         Accounts.Add(Database.AutoSave.AddValue(ItemId, itemName: account.ToString(), containerName: ToString(), needsReview: false, account));

         account.Passwords[DateTime.Now] = Database.AutoSave.UpdateValue(account.ItemId,
            itemName: account.ToString(),
            fieldName: nameof(account.Password),
            needsReview: true,
            oldValue: string.Empty,
            newValue: account.Password,
            readableValue: string.Empty);

         return account;
      }

      public IAccount AddAccount(string label, IEnumerable<string> identifiers)
      {
         return AddAccount(label, identifiers, password: string.Empty);
      }

      public IAccount AddAccount(IEnumerable<string> identifiers, string password)
      {
         return AddAccount(label: string.Empty, identifiers, password);
      }

      public IAccount AddAccount(IEnumerable<string> identifiers)
      {
         return AddAccount(label: string.Empty, identifiers, password: string.Empty);
      }

      void IService.DeleteAccount(IAccount account)
      {
         Account accountToRemove = Accounts.FirstOrDefault(x => x.ItemId == account.ItemId)
            ?? throw new KeyNotFoundException($"The '{account.ItemId}' account was not found into the '{ItemId}' service");

         _ = Accounts.Remove(Database.AutoSave.DeleteValue(ItemId, itemName: accountToRemove.ToString(), containerName: ToString(), needsReview: true, accountToRemove));

      }

      #endregion

      internal Database Database => User.Database;

      public string ItemId { get; set; } = string.Empty;

      internal User User
      {
         get => field ?? throw new NullReferenceException(nameof(User));
         set
         {
            field = value;

            foreach (Account account in Accounts)
            {
               account.Service = this;
            }
         }
      }

      public List<Account> Accounts { get; set; } = [];

      public string ServiceName { get; set; } = string.Empty;
      public string Url { get; set; } = string.Empty;
      public string Notes { get; set; } = string.Empty;

      public void Apply(Change change)
      {
         switch (change.ItemId.Length / Database.CryptographyCenter.HashLength)
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
            case Change.Type.Update:
               switch (change.FieldName)
               {
                  case nameof(ServiceName):
                     ServiceName = change.NewValue.DeserializeTo<string>(Database.SerializationCenter);
                     break;
                  case nameof(Url):
                     Url = change.NewValue.DeserializeTo<string>(Database.SerializationCenter);
                     break;
                  case nameof(Notes):
                     Notes = change.NewValue.DeserializeTo<string>(Database.SerializationCenter);
                     break;
                  default:
                     throw new InvalidDataException("FieldName not valid");
               }
               break;
            case Change.Type.Add:
               Account accountToAdd = change.NewValue.DeserializeTo<Account>(Database.SerializationCenter);
               accountToAdd.Service = this;
               Accounts.Add(accountToAdd);
               break;
            case Change.Type.Delete:
               Account accountToDelete = change.NewValue.DeserializeTo<Account>(Database.SerializationCenter);
               _ = Accounts.RemoveAll(x => x.ItemId == accountToDelete.ItemId);
               break;
            default:
               throw new InvalidEnumArgumentException(nameof(change.ActionType), (int)change.ActionType, typeof(Change.Type));
         }
      }

      public override string ToString() => $"Service {ServiceName}";
   }
}