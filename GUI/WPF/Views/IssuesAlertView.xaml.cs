using System.Windows;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Alerts;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Shared issues list alert (passkey quality, security settings, …) with a parameterized footer.
   /// </summary>
   internal partial class IssuesAlertView : Window, IDisposable, ILanguageAware
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

         switch (viewModel)
         {
            case PasskeyQualityAlertViewModel passkey:
               passkey.OpenUserSettingsRequested += (_, _) => UserSettingsView.ShowUserSettings();
               passkey.OpenAppSettingsRequested += (_, _) => AppSettingsView.ShowAppSettings();
               passkey.CloseRequested += (_, _) => Close();
               break;
            case SecuritySettingsAlertViewModel security:
               security.OpenUserSettingsRequested += (_, _) => UserSettingsView.ShowUserSettings();
               security.OpenAppSettingsRequested += (_, _) => AppSettingsView.ShowAppSettings();
               security.CloseRequested += (_, _) => Close();
               break;
         }
      }

      public void OnLanguageChanged()
      {
         if (_viewModel is ILanguageRefreshable languageAware)
         {
            languageAware.OnLanguageChanged();
         }
      }

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

   /// <summary>Passkey-quality issues alert; distinct type for <see cref="Services.IWindowDialogService"/> singletons.</summary>
   internal sealed class PasskeyQualityAlertView : IssuesAlertView
   {
      internal PasskeyQualityAlertView()
         : base(new PasskeyQualityAlertViewModel(), showAppSettingsButton: false)
      {
      }
   }

   /// <summary>Security-settings issues alert; distinct type for <see cref="Services.IWindowDialogService"/> singletons.</summary>
   internal sealed class SecuritySettingsAlertView : IssuesAlertView
   {
      internal SecuritySettingsAlertView()
         : base(new SecuritySettingsAlertViewModel(), showAppSettingsButton: true)
      {
      }
   }
}
