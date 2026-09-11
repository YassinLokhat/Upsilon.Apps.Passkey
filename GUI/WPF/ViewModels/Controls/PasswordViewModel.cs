using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Utils;
using Upsilon.Apps.Passkey.GUI.WPF.Views;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
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
            AppServices.Clipboard.SetText(Password, ClipboardManager.AutoClearAfter));
         ShowQrCodeCommand = new RelayCommand(() =>
            QrCodeView.ShowQrCode(null, Password, AppServices.Session.User?.Settings.ShowPasswordDelay ?? 0));
      }

      public void Clear()
      {
         Password = string.Empty;
      }
   }
}
