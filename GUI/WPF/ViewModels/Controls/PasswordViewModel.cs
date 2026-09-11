using Upsilon.Apps.Passkey.GUI.WPF.Helper;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal sealed class PasswordViewModel(string updateDate, string password) : ObservableObject
   {
      public string UpdateDate { get; set; } = updateDate;
      public string Password
      {
         get;
         set => SetProperty(ref field, value);
      } = password;

      public void Clear()
      {
         Password = string.Empty;
      }
   }
}
