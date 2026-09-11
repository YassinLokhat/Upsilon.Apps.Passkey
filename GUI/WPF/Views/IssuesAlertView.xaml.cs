using System.Windows;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Shared issues list alert (passkey quality, security settings, …) with a parameterized footer.
   /// </summary>
   internal partial class IssuesAlertView : Window, IDisposable
   {
      private readonly IDisposable _viewModel;
      private bool _disposed;

      protected IssuesAlertView(IDisposable viewModel, bool showAppSettingsButton)
      {
         ArgumentNullException.ThrowIfNull(viewModel);

         InitializeComponent();
         DataContext = _viewModel = viewModel;
         _openAppSettings_BT.Visibility = showAppSettingsButton
            ? Visibility.Visible
            : Visibility.Collapsed;
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

   /// <summary>Passkey-quality issues alert; distinct type for <see cref="Services.IDialogService"/> singletons.</summary>
   internal sealed class PasskeyQualityAlertView : IssuesAlertView
   {
      internal PasskeyQualityAlertView()
         : base(new PasskeyQualityAlertViewModel(), showAppSettingsButton: false)
      {
      }
   }

   /// <summary>Security-settings issues alert; distinct type for <see cref="Services.IDialogService"/> singletons.</summary>
   internal sealed class SecuritySettingsAlertView : IssuesAlertView
   {
      internal SecuritySettingsAlertView()
         : base(new SecuritySettingsAlertViewModel(), showAppSettingsButton: true)
      {
      }
   }
}
