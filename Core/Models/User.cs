using System.ComponentModel;
using Upsilon.Apps.Passkey.Core.Interfaces;
using Upsilon.Apps.PassKey.Core.Enums;

namespace Upsilon.Apps.Passkey.Core.Models
{
   internal sealed class User : IUser, IChangable
   {
      #region IUser interface explicit implementation

      string IItem.ItemId => ItemId;
      IEnumerable<IService> IUser.Services => Services.Cast<IService>();

      string IUser.Username
      {
         get => Username;
         set => Username = Database.AutoSave.UpdateValue(ItemId, nameof(Username), value);
      }

      string[] IUser.Passkeys
      {
         get => Passkeys;
         set => Passkeys = Database.AutoSave.UpdateValue(ItemId, nameof(Passkeys), value);
      }

      int IUser.LogoutTimeout
      {
         get => LogoutTimeout;
         set => LogoutTimeout = Database.AutoSave.UpdateValue(ItemId, nameof(LogoutTimeout), value);
      }

      int IUser.CleaningClipboardTimeout
      {
         get => CleaningClipboardTimeout;
         set => CleaningClipboardTimeout = Database.AutoSave.UpdateValue(ItemId, nameof(CleaningClipboardTimeout), value);
      }

      IService IUser.AddService(string serviceName)
      {
         Service service = new()
         {
            User = this,
            ItemId = ItemId + Database.CryptographicCenter.GetHash(serviceName),
            ServiceName = serviceName,
         };

         Services.Add(Database.AutoSave.AddValue(ItemId, service));

         return service;
      }

      void IUser.DeleteService(IService service)
      {
         Service serviceToRemove = Services.FirstOrDefault(x => x.ItemId == service.ItemId)
            ?? throw new KeyNotFoundException($"The '{service.ItemId}' service was not found into the '{ItemId}' user");

         _ = Services.Remove(Database.AutoSave.DeleteValue(ItemId, serviceToRemove));
      }

      #endregion

      private Database? _database;
      internal Database Database
      {
         get => _database ?? throw new NullReferenceException(nameof(Database));
         set => _database = value;
      }

      public string ItemId { get; set; } = string.Empty;
      public List<Service> Services { get; set; } = [];

      public string Username { get; set; } = string.Empty;
      public string[] Passkeys { get; set; } = [];
      public int LogoutTimeout { get; set; } = 0;
      public int CleaningClipboardTimeout { get; set; } = 0;

      public void Apply(Change change)
      {
         switch (change.ItemId.Length / Database.CryptographicCenter.HashLength)
         {
            case 1:
               _apply(change);
               break;
            case 2:
            case 3:
               Service service = Services.FirstOrDefault(x => change.ItemId.StartsWith(x.ItemId))
                  ?? throw new KeyNotFoundException($"The '{change.ItemId[..(2 * Database.CryptographicCenter.HashLength)]}' service was not found into the '{ItemId}' user");

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
            case ChangeType.Update:
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
                  default:
                     throw new InvalidDataException("FieldName not valid");
               }
               break;
            case ChangeType.Add:
               Service serviceToAdd = Database.SerializationCenter.Deserialize<Service>(change.Value);
               Services.Add(serviceToAdd);
               break;
            case ChangeType.Delete:
               Service serviceToDelete = Database.SerializationCenter.Deserialize<Service>(change.Value);
               _ = Services.RemoveAll(x => x.ItemId == serviceToDelete.ItemId);
               break;
            default:
               throw new InvalidEnumArgumentException(nameof(change.ActionType), (int)change.ActionType, typeof(ChangeType));
         }
      }
   }
}