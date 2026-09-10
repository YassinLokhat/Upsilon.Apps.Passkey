using System.Windows;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   internal sealed partial class SecuritySettingsAlertView : Window, IDisposable
   {
      private readonly SecuritySettingsAlertViewModel _viewModel;
      private bool _disposed;

      internal SecuritySettingsAlertView()
      {
         InitializeComponent();
         DataContext = _viewModel = new SecuritySettingsAlertViewModel();
         Loaded += (_, _) => this.PostLoadSetup();
         Closed += (_, _) => Dispose();
      }

      private void _openUserSettings_Click(object sender, RoutedEventArgs e)
      {
         UserSettingsView.ShowUserSettings(Owner ?? this);
         Close();
      }

      private void _openAppSettings_Click(object sender, RoutedEventArgs e)
      {
         AppSettingsView.ShowAppSettings(Owner ?? this);
         Close();
      }

      private void _ok_Click(object sender, RoutedEventArgs e)
         => Close();

      public void Dispose()
      {
         if (_disposed)
         {
            return;
         }

         _disposed = true;
         _viewModel.Dispose();
         GC.SuppressFinalize(this);
      }
   }
}
