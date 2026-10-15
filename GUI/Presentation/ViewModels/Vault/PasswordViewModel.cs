using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using System.Windows.Input;

namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Vault
{
   internal sealed class PasswordViewModel : ObservableObject
   {
      public string UpdateDate { get; set; }
      public string Password
      {
         get;
         set => SetProperty(ref field, value);
      }

      public ICommand CopyCommand { get; }
      public ICommand ShowQrCodeCommand { get; }

      public PasswordViewModel(string updateDate, string password)
      {
         UpdateDate = updateDate;
         Password = password;
         CopyCommand = new RelayCommand(() =>
            AppServices.Clipboard?.SetText(Password, ClipboardPolicy.AutoClearAfter));
         ShowQrCodeCommand = new RelayCommand(() =>
            _ = ClipboardPolicy.ShowQrCodeAsync(Password));
      }

      public void Clear()
      {
         Password = string.Empty;
      }
   }
}
