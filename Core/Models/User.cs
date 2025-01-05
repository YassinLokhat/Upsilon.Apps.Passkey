using System.ComponentModel;
using Upsilon.Apps.Passkey.Core.Interfaces;
using Upsilon.Apps.PassKey.Core.Enums;
using Upsilon.Apps.PassKey.Core.Utils;

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

      int IUser.PasswordTimeout
      {
         get => PasswordTimeout;
         set => PasswordTimeout = Database.AutoSave.UpdateValue(ItemId, nameof(PasswordTimeout), value);
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

      void IUser.AddService(string serviceName)
      {
         Service service = new()
         {
            User = this,
            ItemId = ItemId + serviceName.GetHash(),
            ServiceName = serviceName,
         };

         Services.Add(Database.AutoSave.AddValue(ItemId, service));
      }

      void IUser.DeleteService(string serviceId)
      {
         Service service = Services.FirstOrDefault(x => x.ItemId == serviceId)
            ?? throw new KeyNotFoundException($"The '{serviceId}' service was not found into the '{ItemId}' user");

         _ = Services.Remove(Database.AutoSave.DeleteValue(ItemId, service));
      }

      #endregion

      private Database? _database;
      internal Database Database
      {
         get => _database ?? throw new NullReferenceException(nameof(Database));
         set => _database = value;
      }

      public string ItemId { get; set; } = string.Empty;
      public List<Service> Services { get; set; } = new();

      public string Username { get; set; } = string.Empty;
      public string[] Passkeys { get; set; } = Array.Empty<string>();
      public int PasswordTimeout { get; set; } = 0;
      public int LogoutTimeout { get; set; } = 0;
      public int CleaningClipboardTimeout { get; set; } = 0;

      public void Apply(Change change)
      {
         switch (change.ItemId.Length / SecurityCenter.HashLength)
         {
            case 1:
               _apply(change);
               break;
            case 2:
            case 3:
               Service service = Services.FirstOrDefault(x => change.ItemId.StartsWith(x.ItemId))
                  ?? throw new KeyNotFoundException($"The '{change.ItemId[..(2 * SecurityCenter.HashLength)]}' service was not found into the '{ItemId}' user");

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
                     Username = change.Value.Deserialize<string>();
                     break;
                  case nameof(Passkeys):
                     Passkeys = change.Value.Deserialize<string[]>();
                     break;
                  case nameof(PasswordTimeout):
                     PasswordTimeout = change.Value.Deserialize<int>();
                     break;
                  case nameof(LogoutTimeout):
                     LogoutTimeout = change.Value.Deserialize<int>();
                     break;
                  case nameof(CleaningClipboardTimeout):
                     CleaningClipboardTimeout = change.Value.Deserialize<int>();
                     break;
                  default:
                     throw new InvalidDataException("FieldName not valid");
               }
               break;
            case ChangeType.Add:
               Service serviceToAdd = change.Value.Deserialize<Service>();
               Services.Add(serviceToAdd);
               break;
            case ChangeType.Delete:
               Service serviceToDelete = change.Value.Deserialize<Service>();
               _ = Services.RemoveAll(x => x.ItemId == serviceToDelete.ItemId);
               break;
            default:
               throw new InvalidEnumArgumentException(nameof(change.ActionType), (int)change.ActionType, typeof(ChangeType));
         }
      }
   }
}