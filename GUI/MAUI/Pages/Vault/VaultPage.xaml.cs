using static Upsilon.Apps.Passkey.GUI.Presentation.Helper.IItemHelper;
using Upsilon.Apps.Passkey.GUI.Presentation.Alerts;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Vault;
using Upsilon.Apps.Passkey.Interfaces.Events;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Pages.Vault
{
   /// <summary>
   /// Services and accounts editor: master-detail from <see cref="WIDE_LAYOUT_MIN_WIDTH"/>,
   /// a list that pushes <see cref="ServiceDetailPage"/> below it.
   /// </summary>
   [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "The navigation stack owns the page; the ViewModel is released when the database closes.")]
   internal sealed partial class VaultPage : ContentPage
   {
      private const double WIDE_LAYOUT_MIN_WIDTH = 800;

      private readonly IDatabase _database;
      private readonly UserServicesViewModel _viewModel;
      private bool? _isWide;
      private bool _closed;

      public VaultPage(IDatabase database)
      {
         ArgumentNullException.ThrowIfNull(database);
         InitializeComponent();

         _database = database;
         BindingContext = _viewModel = new UserServicesViewModel($"{database.User}");

         _viewModel.FiltersRefreshed += (_, _) => _showDetail(_viewModel.SelectedService);
         _viewModel.SaveRequested += async (_, _) => await _viewModel.SaveAsync().ConfigureAwait(true);
         _viewModel.GeneratePasswordRequested += async (_, _) => await _viewModel.GeneratePasswordIntoSelectionAsync().ConfigureAwait(true);
         _viewModel.FocusFilterRequested += (_, _) => _ = ServiceFilterBar.Focus();
         _viewModel.CopyIdentifierRequested += (_, _) => _copy(_viewModel.CurrentIdentifier);
         _viewModel.CopyPasswordRequested += (_, _) => _copy(_viewModel.CurrentPassword);
         _viewModel.LogoutRequested += (_, _) => _database.Close();

         _database.DatabaseClosed += _onDatabaseClosed;
         AppServices.Session.Alerts.NotifiedAlertsChanged += _onNotifiedAlertsChanged;
         AppServices.Navigation.ItemRequested += _onItemRequested;

         if (!AppServices.Capabilities.SupportsKeyboardShortcuts)
         {
            _addToolbarItems();
         }

         // Leaving the vault goes through Log out (or auto-logout), never a bare back navigation.
         Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false, IsEnabled = false });

         _refreshAlerts();
      }

      protected override bool OnBackButtonPressed()
      {
         _database.Close();
         return true;
      }

      protected override void OnSizeAllocated(double width, double height)
      {
         base.OnSizeAllocated(width, height);
         _applyLayout(width >= WIDE_LAYOUT_MIN_WIDTH);
      }

      protected override void OnAppearing()
      {
         base.OnAppearing();

         if (_isWide == false)
         {
            ServicesList.SelectedItem = null;
         }
      }

      private void _applyLayout(bool wide)
      {
         if (_isWide == wide)
         {
            return;
         }

         _isWide = wide;
         ContentGrid.ColumnDefinitions.Clear();
         ContentGrid.ColumnDefinitions.Add(new ColumnDefinition(wide ? new GridLength(320) : GridLength.Star));

         if (wide)
         {
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            Grid.SetColumn(DetailHost, 1);
         }

         DetailHost.IsVisible = wide;
         _showDetail(_viewModel.SelectedService);
      }

      private void _showDetail(ServiceViewModel? service)
      {
         DetailEditor.BindingContext = service;
         DetailEditor.IsVisible = service is not null;
         NoSelectionLabel.IsVisible = service is null;
      }

      private async void _onServiceSelectionChanged(object? sender, SelectionChangedEventArgs e)
      {
         AppServices.Session.User?.Shake();

         if (_isWide != false)
         {
            _showDetail(_viewModel.SelectedService);
            return;
         }

         if (_viewModel.SelectedService is { } service)
         {
            await Navigation.PushAsync(new ServiceDetailPage(_viewModel, service)).ConfigureAwait(true);
         }
      }

      private void _onDeleteService(object? sender, EventArgs e)
      {
         if ((sender as BindableObject)?.BindingContext is ServiceViewModel service)
         {
            _viewModel.SelectedService = service;
            _viewModel.DeleteServiceCommand.Execute(null);
         }
      }

      private async void _onShowAlerts(object? sender, EventArgs e)
      {
         AlertGroupSummary[] groups = [.. _viewModel.AlertGroups.Where(g => g.Count != 0)];
         string[] labels = [.. groups.Select(_labelFor)];

         string? choice = await DisplayActionSheetAsync(_viewModel.ShowAlerts, Strings.Button_Cancel, null, labels).ConfigureAwait(true);
         int index = Array.IndexOf(labels, choice);

         if (index >= 0)
         {
            _viewModel.CommandFor(groups[index].Group).Execute(null);
         }
      }

      private string _labelFor(AlertGroupSummary group) => group.Group switch
      {
         AlertGroup.LeakedPasswords => _viewModel.ShowLeakedPasswordAlerts,
         AlertGroup.PasskeyQuality => _viewModel.ShowPasskeyQualityAlerts,
         AlertGroup.ExpiredPasswords => _viewModel.ShowExpiredPasswordAlerts,
         AlertGroup.WeakPasswords => _viewModel.ShowWeakPasswordAlerts,
         AlertGroup.ActivityReview => _viewModel.ShowActivityAlerts,
         AlertGroup.DuplicatedPasswords => _viewModel.ShowDuplicatedPasswordAlerts,
         _ => _viewModel.ShowSecuritySettingsAlerts,
      };

      private async void _onGeneratePassword(object? sender, EventArgs e)
         => await _viewModel.GeneratePasswordIntoSelectionAsync().ConfigureAwait(true);

      private static void _copy(string? text)
      {
         if (!string.IsNullOrEmpty(text))
         {
            AppServices.Clipboard?.SetText(text, ClipboardPolicy.AutoClearAfter);
         }
      }

      private void _addToolbarItems()
      {
         ToolbarItems.Add(new ToolbarItem { Text = Strings.Menu_Save, Command = _viewModel.SaveCommand });
         ToolbarItems.Add(new ToolbarItem { Text = Strings.Vault_AddService, Command = _viewModel.AddServiceCommand });
         ToolbarItems.Add(new ToolbarItem { Text = Strings.Menu_Undo, Command = _viewModel.UndoCommand, Order = ToolbarItemOrder.Secondary });
         ToolbarItems.Add(new ToolbarItem { Text = Strings.Menu_Redo, Command = _viewModel.RedoCommand, Order = ToolbarItemOrder.Secondary });
         ToolbarItems.Add(new ToolbarItem { Text = Strings.Menu_GenerateRandomPassword, Command = _viewModel.GeneratePasswordCommand, Order = ToolbarItemOrder.Secondary });
         ToolbarItems.Add(new ToolbarItem { Text = Strings.Menu_ShowActivities, Command = _viewModel.ShowActivitiesCommand, Order = ToolbarItemOrder.Secondary });
         ToolbarItems.Add(new ToolbarItem { Text = Strings.Menu_UserSettings, Command = _viewModel.UserSettingsCommand, Order = ToolbarItemOrder.Secondary });
         ToolbarItems.Add(new ToolbarItem { Text = Strings.Menu_AppSettings, Command = _viewModel.AppSettingsCommand, Order = ToolbarItemOrder.Secondary });
         ToolbarItems.Add(new ToolbarItem { Text = Strings.Menu_Logout, Command = _viewModel.LogoutCommand, Order = ToolbarItemOrder.Secondary });
      }

      private void _onNotifiedAlertsChanged(object? sender, EventArgs e)
         => MainThread.BeginInvokeOnMainThread(_refreshAlerts);

      private void _refreshAlerts()
      {
         if (!_closed)
         {
            _viewModel.ApplyAlertSummary(AlertSummary.From(AppServices.Session.Alerts.GetNotifiedAlerts()));
         }
      }

      private async void _onItemRequested(object? sender, ItemNavigationEventArgs e)
      {
         if (_closed)
         {
            return;
         }

         if (string.IsNullOrEmpty(e.ItemId) || e.ItemId == _database.User?.ItemId)
         {
            _viewModel.UserSettingsCommand.Execute(null);
            return;
         }

         if (!_viewModel.TrySelectItem(e.ItemId))
         {
            if (AppServices.Dialogs is { } dialogs)
            {
               await dialogs.WarnAsync(Strings.Format(nameof(Strings.Msg_ItemNotFound), e.ItemId), Strings.Title_ItemNotFound)
                  .ConfigureAwait(true);
            }

            return;
         }

         if (_isWide != false)
         {
            ServicesList.ScrollTo(_viewModel.SelectedService);
         }
      }

      private void _onDatabaseClosed(object? sender, LogoutEventArgs e)
      {
         if (_closed)
         {
            return;
         }

         _closed = true;
         _database.DatabaseClosed -= _onDatabaseClosed;
         AppServices.Session.Alerts.NotifiedAlertsChanged -= _onNotifiedAlertsChanged;
         AppServices.Navigation.ItemRequested -= _onItemRequested;

         MainThread.BeginInvokeOnMainThread(_release);
      }

      private void _release()
      {
         DetailEditor.BindingContext = null;
         _viewModel.Dispose();
      }
   }
}
