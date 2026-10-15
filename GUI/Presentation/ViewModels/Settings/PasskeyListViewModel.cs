using System.Collections.ObjectModel;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Settings
{
   /// <summary>
   /// Ordered passkey layers being edited. Stored passkeys are kept protected
   /// until a host control takes them; plaintext only exists in those controls
   /// and in the transient array returned by <see cref="RevealAll"/>.
   /// </summary>
   internal sealed class PasskeyListViewModel : ObservableObject, IDisposable
   {
      private bool _disposed;

      public PasskeyListViewModel(IUser? user)
      {
         AddCommand = new RelayCommand(() => _ = Add());

         string[] stored = user is null ? [] : [.. user.Passkeys];
         for (int i = 0; i < stored.Length; i++)
         {
            _add(AppServices.SecretProtector.Protect(stored[i]), i);
            stored[i] = string.Empty;
         }

         if (Entries.Count == 0)
         {
            _ = Add();
         }
      }

      public ObservableCollection<PasskeyEntryViewModel> Entries { get; } = [];

      public ICommand AddCommand { get; }

      /// <summary>Raised after a row is added so the host can focus it.</summary>
      public event EventHandler<PasskeyEntryViewModel>? EntryAdded;

      public PasskeyEntryViewModel Add() => _add(null, null);

      public bool Remove(PasskeyEntryViewModel entry)
      {
         if (Entries.Count <= 1 || !Entries.Remove(entry))
         {
            return false;
         }

         entry.Dispose();
         _reindex();
         return true;
      }

      public bool Move(PasskeyEntryViewModel entry, int offset)
      {
         int index = Entries.IndexOf(entry);
         int target = index + offset;

         if (index < 0 || target < 0 || target >= Entries.Count)
         {
            return false;
         }

         Entries.Move(index, target);
         _reindex();
         return true;
      }

      /// <summary>Plaintext layers for one save; the caller drops the array right after.</summary>
      public string[] RevealAll() => [.. Entries.Select(e => e.Reveal())];

      /// <summary>Localized reason the current rows cannot be saved, or empty.</summary>
      public string ValidationError()
      {
         if (Entries.Count == 0)
         {
            return Strings.Msg_AtLeastOnePassword;
         }

         foreach (PasskeyEntryViewModel entry in Entries)
         {
            if (string.IsNullOrEmpty(entry.Reveal()))
            {
               return Strings.Msg_NoPasswordEmpty;
            }
         }

         return string.Empty;
      }

      public void ClearSecrets()
      {
         foreach (PasskeyEntryViewModel entry in Entries)
         {
            entry.Clear();
         }
      }

      public void OnThemeChanged()
      {
         foreach (PasskeyEntryViewModel entry in Entries)
         {
            entry.OnThemeChanged();
         }
      }

      public void Dispose()
      {
         if (_disposed)
         {
            return;
         }

         _disposed = true;
         foreach (PasskeyEntryViewModel entry in Entries)
         {
            entry.Dispose();
         }

         Entries.Clear();
         EntryAdded = null;
      }

      private PasskeyEntryViewModel _add(Interfaces.Utils.IProtectedSecret? seed, int? storedIndex)
      {
         PasskeyEntryViewModel entry = new(seed, storedIndex) { Index = Entries.Count };
         entry.UpRequested += (_, _) => _ = Move(entry, -1);
         entry.DownRequested += (_, _) => _ = Move(entry, 1);
         entry.DeleteRequested += (_, _) => _ = Remove(entry);
         Entries.Add(entry);
         EntryAdded?.Invoke(this, entry);
         return entry;
      }

      private void _reindex()
      {
         for (int i = 0; i < Entries.Count; i++)
         {
            Entries[i].Index = i;
         }
      }
   }
}
