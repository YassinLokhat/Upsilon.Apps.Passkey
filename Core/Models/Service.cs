﻿using System.ComponentModel;
using Upsilon.Apps.Passkey.Core.Interfaces;
using Upsilon.Apps.PassKey.Core.Enums;

namespace Upsilon.Apps.Passkey.Core.Models
{
   internal sealed class Service : IService, IChangable
   {
      #region IService interface explicit implementation

      string IItem.ItemId => ItemId;
      IUser IService.User => User;
      IEnumerable<IAccount> IService.Accounts => Accounts.Cast<IAccount>();

      string IService.ServiceName
      {
         get => ServiceName;
         set => ServiceName = Database.AutoSave.UpdateValue(ItemId, nameof(ServiceName), value);
      }

      string IService.Url
      {
         get => Url;
         set => Url = Database.AutoSave.UpdateValue(ItemId, nameof(Url), value);
      }

      string IService.Notes
      {
         get => Notes;
         set => Notes = Database.AutoSave.UpdateValue(ItemId, nameof(Notes), value);
      }

      void IService.AddAccount(string label, IEnumerable<string> identifiants, string password)
      {
         Account account = new()
         {
            Service = this,
            ItemId = ItemId + Database.CryptographicCenter.GetHash(label + string.Join(string.Empty, identifiants)),
            Label = label,
            Identifiants = identifiants.ToArray(),
            Password = password,
         };

         Accounts.Add(Database.AutoSave.AddValue(ItemId, account));
      }

      void IService.DeleteAccount(string accountId)
      {
         Account account = Accounts.FirstOrDefault(x => x.ItemId == accountId)
            ?? throw new KeyNotFoundException($"The '{accountId}' account was not found into the '{ItemId}' service");

         _ = Accounts.Remove(Database.AutoSave.DeleteValue(ItemId, account));
      }

      #endregion

      internal Database Database => User.Database;

      public string ItemId { get; set; } = string.Empty;

      private User? _user;
      internal User User
      {
         get => _user ?? throw new NullReferenceException(nameof(User));
         set => _user = value;
      }

      public List<Account> Accounts { get; set; } = [];

      public string ServiceName { get; set; } = string.Empty;
      public string Url { get; set; } = string.Empty;
      public string Notes { get; set; } = string.Empty;

      public void Apply(Change change)
      {
         switch (change.ItemId.Length / Database.CryptographicCenter.HashLength)
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
                     ServiceName = Database.SerializationCenter.Deserialize<string>(change.Value); ;
                     break;
                  case nameof(Url):
                     Url = Database.SerializationCenter.Deserialize<string>(change.Value); ;
                     break;
                  case nameof(Notes):
                     Notes = Database.SerializationCenter.Deserialize<string>(change.Value); ;
                     break;
                  default:
                     throw new InvalidDataException("FieldName not valid");
               }
               break;
            case ChangeType.Add:
               Account accountToAdd = Database.SerializationCenter.Deserialize<Account>(change.Value);
               Accounts.Add(accountToAdd);
               break;
            case ChangeType.Delete:
               Account accountToDelete = Database.SerializationCenter.Deserialize<Account>(change.Value);
               _ = Accounts.RemoveAll(x => x.ItemId == accountToDelete.ItemId);
               break;
            default:
               throw new InvalidEnumArgumentException(nameof(change.ActionType), (int)change.ActionType, typeof(ChangeType));
         }
      }
   }
}