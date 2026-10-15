using System.Diagnostics.CodeAnalysis;
using Upsilon.Apps.Passkey.GUI.MAUI.Adapters;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Settings;
using Upsilon.Apps.Passkey.Interfaces.Models;
using static Upsilon.Apps.Passkey.GUI.Presentation.Helper.IItemHelper;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Pages.Settings
{
   [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "The ViewModel is disposed when the modal completes; pages are never reused.")]
   internal sealed partial class UserSettingsPage : ContentPage, IResultPage<bool>
   {
      private readonly TaskCompletionSource<bool> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
      private UserSettingsViewModel? _viewModel;
      private IDatabase? _database;
      private bool _started;

      public UserSettingsPage()
      {
         InitializeComponent();
      }

      public Task<bool> Completion => _completion.Task;

      protected override async void OnAppearing()
      {
         base.OnAppearing();
         if (_started)
         {
            return;
         }

         _started = true;

         // The ViewModel seeds the passkey rows, so it is only built once the stored credentials are confirmed.
         if (AppServices.Session.Database?.User is not null
            && !await UserSettingsViewModel.ConfirmStoredCredentialsAsync().ConfigureAwait(true))
         {
            _complete(false);
            return;
         }

         _database = AppServices.Session.Database;
         if (_database?.User is { } user)
         {
            user.Shake();
            _database.DatabaseClosed += _onDatabaseClosed;
         }

         _viewModel = new UserSettingsViewModel();
         _viewModel.CloseRequested += _onCloseRequested;
         BindingContext = _viewModel;
      }

      protected override bool OnBackButtonPressed()
      {
         if (_viewModel?.IsBusy == true)
         {
            return true;
         }

         _complete(false);
         return true;
      }

      private void _onClose(object? sender, EventArgs e)
      {
         if (_viewModel?.IsBusy != true)
         {
            _complete(false);
         }
      }

      private void _onCloseRequested(object? sender, EventArgs e) => _complete(true);

      private void _onDatabaseClosed(object? sender, EventArgs e)
         => MainThread.BeginInvokeOnMainThread(() => _complete(false));

      private void _complete(bool result)
      {
         if (!_completion.TrySetResult(result))
         {
            return;
         }

         if (_database is not null)
         {
            _database.DatabaseClosed -= _onDatabaseClosed;
            _database = null;
         }

         if (_viewModel is not null)
         {
            _viewModel.CloseRequested -= _onCloseRequested;
            BindingContext = null;
            _viewModel.Dispose();
            _viewModel = null;
         }
      }
   }
}
