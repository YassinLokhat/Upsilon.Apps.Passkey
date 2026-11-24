using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Settings;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views.Controls
{
   /// <summary>
   /// Interaction logic for UserPasswordItem.xaml. The PasswordBox is the only
   /// live copy of the passkey; the ViewModel reads it through <see cref="ISecretInput"/>.
   /// </summary>
   internal sealed partial class UserPasswordItem : UserControl, ISecretInput
   {
      internal readonly PasskeyEntryViewModel ViewModel;

      internal UserPasswordItem(PasskeyEntryViewModel viewModel)
      {
         InitializeComponent();

         DataContext = ViewModel = viewModel;
         viewModel.Attach(this);
      }

      public string Read() => _password_VPB.Password;

      public void Write(string secret) => _password_VPB.Password = secret;

      public void Clear() => _password_VPB.Clear();

      public new void Focus() => _password_VPB.Focus();
   }
}
