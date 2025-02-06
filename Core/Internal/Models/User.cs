using System.ComponentModel;
using Upsilon.Apps.PassKey.Core.Public.Enums;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.PassKey.Core.Internal.Models
{
   internal sealed class User : IUser
   {
      #region IUser interface explicit Internal

      string IItem.ItemId => ItemId;
      IService[] IUser.Services => [.. Services];

      string IUser.Username
      {
         get => Username;
         set => Username = Database.AutoSave.UpdateValue(ItemId,
            itemName: ToString(),
            fieldName: nameof(Username),
            needsReview: true,
            value: value,
            readableValue: value);
      }

      string[] IUser.Passkeys
      {
         get => Passkeys;
         set => Passkeys = Database.AutoSave.UpdateValue(ItemId,
            itemName: ToString(),
            fieldName: nameof(Passkeys),
            needsReview: true,
            value: value,
            readableValue: string.Empty);
      }

      int IUser.LogoutTimeout
      {
         get => LogoutTimeout;
         set => LogoutTimeout = Database.AutoSave.UpdateValue(ItemId,
            itemName: ToString(),
            fieldName: nameof(LogoutTimeout),
            needsReview: false,
            value: value,
            readableValue: value.ToString());
      }

      int IUser.CleaningClipboardTimeout
      {
         get => CleaningClipboardTimeout;
         set => CleaningClipboardTimeout = Database.AutoSave.UpdateValue(ItemId,
            itemName: ToString(),
            fieldName: nameof(CleaningClipboardTimeout),
            needsReview: false,
            value: value,
            readableValue: value.ToString());
      }

      WarningType IUser.WarningsToNotify
      {
         get => WarningsToNotify;
         set => WarningsToNotify = Database.AutoSave.UpdateValue(ItemId,
            itemName: ToString(),
            fieldName: nameof(WarningsToNotify),
            needsReview: true,
            value: value,
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

      private Database? _database;
      internal Database Database
      {
         get => _database ?? throw new NullReferenceException(nameof(Database));
         set
         {
            _database = value;

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
      public int LogoutTimeout { get; set; } = 0;
      public int CleaningClipboardTimeout { get; set; } = 0;
      public WarningType WarningsToNotify { get; set; }
         = WarningType.LogReviewWarning
         | WarningType.PasswordUpdateReminderWarning
         | WarningType.DuplicatedPasswordsWarning
         | WarningType.PasswordLeakedWarning;

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
                     Username = Database.SerializationCenter.Deserialize<string>(change.Value);
                     break;
                  case nameof(Passkeys):
                     Passkeys = Database.SerializationCenter.Deserialize<string[]>(change.Value);
                     break;
                  case nameof(LogoutTimeout):
                     LogoutTimeout = Database.SerializationCenter.Deserialize<int>(change.Value);
                     break;
                  case nameof(CleaningClipboardTimeout):
                     CleaningClipboardTimeout = Database.SerializationCenter.Deserialize<int>(change.Value);
                     break;
                  case nameof(WarningsToNotify):
                     WarningsToNotify = Database.SerializationCenter.Deserialize<WarningType>(change.Value);
                     break;
                  default:
                     throw new InvalidDataException("FieldName not valid");
               }
               break;
            case Change.Type.Add:
               Service serviceToAdd = Database.SerializationCenter.Deserialize<Service>(change.Value);
               serviceToAdd.User = this;
               Services.Add(serviceToAdd);
               break;
            case Change.Type.Delete:
               Service serviceToDelete = Database.SerializationCenter.Deserialize<Service>(change.Value);
               _ = Services.RemoveAll(x => x.ItemId == serviceToDelete.ItemId);
               break;
            default:
               throw new InvalidEnumArgumentException(nameof(change.ActionType), (int)change.ActionType, typeof(Change.Type));
         }
      }

      public override string ToString() => $"User {Database.Username}";
   }
}