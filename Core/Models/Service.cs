using System.ComponentModel;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   /// <summary>
   /// A site/app grouping accounts. Item ids are prefixed <c>S</c>.
   /// </summary>
   internal sealed class Service : IService
   {
      #region IService interface explicit Internal

      string IItem.ItemId => Host.Touch(ItemId);

      IDatabase IItem.Database => Host.AsDatabase;

      IUser IService.User => Host.Touch(User);
      IEnumerable<IAccount> IService.Accounts => [.. Host.Touch(Accounts)];

      string IService.ServiceName
      {
         get => Host.Touch(ServiceName);
         set => ServiceName = Host.AutoSave.UpdateValue(ItemId,
            fieldName: nameof(ServiceName),
            needsReview: true,
            oldValue: ServiceName,
            newValue: value,
            readableValue: value);
      }

      Uri? IService.Url
      {
         get => !string.IsNullOrWhiteSpace(Url) ? new Uri(Host.Touch(Url)) : null;
         set => Url = Host.AutoSave.UpdateValue(ItemId,
            fieldName: nameof(Url),
            needsReview: false,
            oldValue: Url,
            newValue: value?.OriginalString ?? string.Empty,
            readableValue: value?.OriginalString ?? string.Empty);
      }

      string IService.Notes
      {
         get => Host.Touch(Notes);
         set => Notes = Host.AutoSave.UpdateValue(ItemId,
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
            ItemId = "A" + Host.CryptographyCenter.GetHash(ItemId + label + string.Join(string.Empty, identifiers)),
            Label = label,
            Identifiers = [.. identifiers],
            Password = password,
         };

         Accounts.Add(Host.AutoSave.AddValue(ItemId, readableValue: account.ToString(), needsReview: false, account));

         _ = Host.AutoSave.UpdateValue(account.ItemId,
            fieldName: nameof(account.Password),
            needsReview: true,
            oldValue: string.Empty,
            newValue: account.Password,
            readableValue: string.Empty);

         account.Passwords[DateTime.Now] = ProtectedSecret.Protect(account.Password);

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
            ?? throw new KeyNotFoundException($"The {account}' was not found into the {this}'s accounts list");

         _ = Accounts.Remove(Host.AutoSave.DeleteValue(ItemId, readableValue: accountToRemove.ToString(), needsReview: true, accountToRemove));
      }

      #endregion

      internal IUserHost Host => User.Host;

      public string ItemId { get; set; } = string.Empty;

      internal User User
      {
         get => field ?? throw new NullValueException(nameof(User));
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

      public IAccount AddAccount(string label, IEnumerable<string> identifiers, string password, Dictionary<DateTime, ProtectedSecret> passwords)
      {
         Account account = new()
         {
            Service = this,
            ItemId = "A" + Host.CryptographyCenter.GetHash(ItemId + label + string.Join(string.Empty, identifiers)),
            Label = label,
            Identifiers = [.. identifiers],
            Password = password,
            Passwords = passwords,
         };

         // CSV/import paths often supply a current password with an empty history
         // dictionary. Seed one dated entry so PasswordExpired and retention work.
         if (account.Passwords.Count == 0
            && !string.IsNullOrEmpty(password))
         {
            account.Passwords[DateTime.Now] = ProtectedSecret.Protect(password);
         }

         Accounts.Add(Host.AutoSave.AddValue(ItemId, readableValue: account.ToString(), needsReview: false, account));

         _ = Host.AutoSave.UpdateValue(account.ItemId,
            fieldName: nameof(account.Password),
            needsReview: false,
            oldValue: string.Empty,
            newValue: account.Password,
            readableValue: string.Empty);

         return account;
      }

      internal void Apply(Change change)
      {
         switch (change.ActionType)
         {
            case ActivityEventType.ItemUpdated:
               switch (change.FieldName)
               {
                  case nameof(ServiceName):
                     ServiceName = change.NewValue.DeserializeTo<string>(Host.SerializationCenter);
                     break;
                  case nameof(Url):
                     Url = change.NewValue.DeserializeTo<string>(Host.SerializationCenter);
                     break;
                  case nameof(Notes):
                     Notes = change.NewValue.DeserializeTo<string>(Host.SerializationCenter);
                     break;
                  default:
                     throw new InvalidDataException("FieldName not valid");
               }
               break;
            case ActivityEventType.ItemAdded:
               Account accountToAdd = change.NewValue.DeserializeTo<Account>(Host.SerializationCenter);
               accountToAdd.Service = this;
               Accounts.Add(accountToAdd);
               break;
            case ActivityEventType.ItemDeleted:
               Account accountToDelete = change.NewValue.DeserializeTo<Account>(Host.SerializationCenter);
               _ = Accounts.RemoveAll(x => x.ItemId == accountToDelete.ItemId);
               break;
            default:
               throw new InvalidEnumArgumentException(nameof(change.ActionType), (int)change.ActionType, typeof(ActivityEventType));
         }
      }

      public override string ToString() => $"Service {ServiceName}";

      public bool HasChanged() => Host.HasPendingChanges(ItemId) || Accounts.Any(x => x.HasChanged());
   }
}
