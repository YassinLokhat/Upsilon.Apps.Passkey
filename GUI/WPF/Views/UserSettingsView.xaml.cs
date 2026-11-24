using static Upsilon.Apps.Passkey.GUI.Presentation.Helper.IItemHelper;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Settings;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.Interfaces.Models;

using AppServices = Upsilon.Apps.Passkey.GUI.WPF.Services.AppServices;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for UserSettingsView.xaml
   /// </summary>
   internal sealed partial class UserSettingsView : Window, IThemeAware, ILanguageAware, IDisposable
   {
      private readonly UserSettingsViewModel _viewModel;
      private readonly IDatabase? _database;
      private bool _isClosing;

      private static ISessionService _session => AppServices.Session;

      public UserSettingsView()
      {
         InitializeComponent();

         _database = _session.Database;
         bool hasUser = _database?.User is not null;

         _deleteUser_MI.Visibility
            = _import_MI.Visibility
            = _export_MI.Visibility
            = _viewActivities_MI.Visibility
            = hasUser ? Visibility.Visible : Visibility.Collapsed;

         DataContext = _viewModel = new UserSettingsViewModel();
         _viewModel.CloseRequested += _viewModel_CloseRequested;
         _viewModel.PropertyChanged += _viewModel_PropertyChanged;
         _passwordsContainer.Bind(_viewModel.Passkeys);

         if (_database?.User is not null)
         {
            _database.User.Shake();
            _database.DatabaseClosed += _database_DatabaseClosed;
         }

         _username_TB.SelectedText = _viewModel.Username;
         _ = _username_TB.Focus();

         Loaded += (s, e) => this.PostLoadSetup();
         Closed += _window_Closed;
      }

      public void OnLanguageChanged()
         => this.ForwardToDataContext();

      public void OnThemeChanged()
         => _viewModel.OnThemeChanged();

      public static async Task ShowUserSettingsAsync()
      {
         if (_session.Database?.User is not null
            && !await UserSettingsViewModel.ConfirmStoredCredentialsAsync().ConfigureAwait(true))
         {
            return;
         }

         using UserSettingsView view = new();
         _ = AppServices.Dialogs.ShowDialog(view);
      }

      public static void ShowUserSettings() => _ = ShowUserSettingsAsync();

      private void _viewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
      {
         if (e.PropertyName == nameof(UserSettingsViewModel.IsBusy))
         {
            this.SetIsBusy(_viewModel.IsBusy);
         }
      }

      private void _viewModel_CloseRequested(object? sender, EventArgs e)
      {
         // Close before the session ends so DatabaseClosed reaches UserServicesView
         // without this modal still open on top.
         _database?.DatabaseClosed -= _database_DatabaseClosed;

         if (!_isClosing && IsLoaded)
         {
            DialogResult = true;
         }
      }

      private void _window_Closed(object? sender, EventArgs e)
      {
         _isClosing = true;
         _database?.DatabaseClosed -= _database_DatabaseClosed;
         _viewModel.CloseRequested -= _viewModel_CloseRequested;
         _viewModel.PropertyChanged -= _viewModel_PropertyChanged;
         _passwordsContainer.Unbind();
         _viewModel.Dispose();
      }

      public void Dispose() => _viewModel.Dispose();

      private void _database_DatabaseClosed(object? sender, Interfaces.Events.LogoutEventArgs e)
          => this.DatabaseClosed(_isClosing);

      private void _value_TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
         => NumericTextBoxHelper.PreviewTextInput(sender, e);

      private void _value_TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
         => NumericTextBoxHelper.Pasting(sender, e);

      private void _value_TextBox_TextChanged(object sender, TextChangedEventArgs e)
         => NumericTextBoxHelper.TextChanged(sender, e);
   }
}
