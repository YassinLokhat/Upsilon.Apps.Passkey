using Microsoft.Win32;
using System.IO;
using System.Windows;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.WPF.Views;
using SharedServices = Upsilon.Apps.Passkey.GUI.Presentation.Services.AppServices;
using IDialogService = Upsilon.Apps.Passkey.GUI.Presentation.Services.IDialogService;
namespace Upsilon.Apps.Passkey.GUI.WPF.Services
{
   /// <summary>
   /// WPF dialogs are modal, so the async <see cref="IDialogService"/> members
   /// complete synchronously once the window closes.
   /// </summary>
   internal sealed class DialogService : IWindowDialogService, IDialogService
   {
      private readonly Dictionary<Type, Window> _singletons = [];

      public Task<DialogResult> ConfirmAsync(string text, string title, DialogButtons buttons = DialogButtons.YesNo, DialogIcon icon = DialogIcon.Question, CancellationToken cancellationToken = default)
         => Task.FromResult(ToDialogResult(Confirm(text, title, ToButton(buttons), ToImage(icon))));

      public Task InfoAsync(string text, string title, CancellationToken cancellationToken = default)
      {
         Info(text, title);
         return Task.CompletedTask;
      }

      public Task WarnAsync(string text, string title, CancellationToken cancellationToken = default)
      {
         Warn(text, title);
         return Task.CompletedTask;
      }

      public Task ErrorAsync(string text, string title, CancellationToken cancellationToken = default)
      {
         Error(text, title);
         return Task.CompletedTask;
      }

      public Task<string?> PickBrowseFolderAsync(string title, string defaultPath, CancellationToken cancellationToken = default)
         => Task.FromResult(PickBrowseFolder(title, defaultPath));

      public Task<string?> PickOpenFileAsync(string filter, string title, CancellationToken cancellationToken = default)
         => Task.FromResult(PickOpenFile(filter, title));

      public Task<string?> PickSaveFileAsync(string filter, string title, string? defaultFileName = null, string? initialDirectory = null, CancellationToken cancellationToken = default)
         => Task.FromResult(PickSaveFile(filter, title, defaultFileName, initialDirectory));

      public Task<TResult?> ShowAsync<TResult>(IViewRequest<TResult> request, CancellationToken cancellationToken = default)
      {
         ArgumentNullException.ThrowIfNull(request);
         cancellationToken.ThrowIfCancellationRequested();

         object? result = request switch
         {
            GeneratePasswordRequest => PasswordGenerator.ShowGeneratePasswordDialog(),
            InsertIdentifierRequest r => InsertIdentifierView.InsertIdentifierDialog(r.Identifiers, r.Current),
            ShowQrCodeRequest r => _done(() => QrCodeView.ShowQrCode(r.Content, r.DelayMilliseconds)),
            ConfirmCredentialsRequest r => CredentialsConfirmationView.ShowConfirmationDialog(r.Credentials, r.IsNew),
            ActivitiesRequest r => _done(() => _showActivities(r)),
            AccountPasswordsAlertRequest r => _done(() => ShowSingleton(
               () => new AccountPasswordsAlertView(r.Kind),
               view => view.SetKind(r.Kind))),
            DuplicatedPasswordsAlertRequest => _done(() => ShowSingleton(() => new DuplicatedPasswordsAlertView())),
            PasskeyQualityAlertRequest => _done(() => ShowSingleton(() => new PasskeyQualityAlertView())),
            SecuritySettingsAlertRequest => _done(() => ShowSingleton(() => new SecuritySettingsAlertView())),
            UserSettingsRequest => _done(UserSettingsView.ShowUserSettings),
            AppSettingsRequest => _done(AppSettingsView.ShowAppSettings),
            _ => _unhandled(request),
         };

         return Task.FromResult(result is TResult typed ? typed : default);
      }

      private void _showActivities(ActivitiesRequest request)
      {
         void apply(UserActivitiesView view)
         {
            if (request.SearchCriteria is not null)
            {
               view.ViewModel.ClearFilters();
               view.ViewModel.SearchCriteria = request.SearchCriteria;
            }

            view.ViewModel.NeedsReview = request.NeedsReview;
         }

         _ = ShowSingleton(
            () =>
            {
               UserActivitiesView view = new(request.NeedsReview);
               apply(view);
               return view;
            },
            apply);
      }

      private static bool _done(Action show)
      {
         show();
         return true;
      }

      private static object? _unhandled(object request)
      {
         SharedServices.Log.Warn($"No WPF view registered for {request.GetType().Name}.");
         return null;
      }

      internal static MessageBoxButton ToButton(DialogButtons buttons) => buttons switch
      {
         DialogButtons.OK => MessageBoxButton.OK,
         DialogButtons.OKCancel => MessageBoxButton.OKCancel,
         DialogButtons.YesNoCancel => MessageBoxButton.YesNoCancel,
         _ => MessageBoxButton.YesNo,
      };

      internal static MessageBoxImage ToImage(DialogIcon icon) => icon switch
      {
         DialogIcon.Information => MessageBoxImage.Information,
         DialogIcon.Warning => MessageBoxImage.Warning,
         DialogIcon.Error => MessageBoxImage.Error,
         DialogIcon.Question => MessageBoxImage.Question,
         _ => MessageBoxImage.None,
      };

      internal static DialogResult ToDialogResult(MessageBoxResult result) => result switch
      {
         MessageBoxResult.OK => DialogResult.OK,
         MessageBoxResult.Cancel => DialogResult.Cancel,
         MessageBoxResult.Yes => DialogResult.Yes,
         MessageBoxResult.No => DialogResult.No,
         _ => DialogResult.None,
      };

      public bool? ShowDialog<TWindow>(TWindow window) where TWindow : Window
      {
         ArgumentNullException.ThrowIfNull(window);

         window.Owner = _resolveOwner();
         return window.ShowDialog();
      }

      public TWindow ShowSingleton<TWindow>(Func<TWindow> factory, Action<TWindow>? configure = null) where TWindow : Window
      {
         ArgumentNullException.ThrowIfNull(factory);

         if (_singletons.TryGetValue(typeof(TWindow), out Window? existing)
            && existing is TWindow loaded
            && loaded.IsLoaded)
         {
            configure?.Invoke(loaded);
            _ = loaded.Activate();
            return loaded;
         }

         TWindow window = factory();

         configure?.Invoke(window);

         window.Closed += (_, _) =>
         {
            if (_singletons.TryGetValue(typeof(TWindow), out Window? tracked) && ReferenceEquals(tracked, window))
            {
               _ = _singletons.Remove(typeof(TWindow));
            }
         };

         _singletons[typeof(TWindow)] = window;
         window.Show();

         return window;
      }

      public TWindow? GetSingleton<TWindow>() where TWindow : Window
      {
         return _singletons.TryGetValue(typeof(TWindow), out Window? window) ? window as TWindow : null;
      }

      public void Close<TWindow>() where TWindow : Window
      {
         if (_singletons.TryGetValue(typeof(TWindow), out Window? window))
         {
            _ = _singletons.Remove(typeof(TWindow));
            window.Close();
         }
      }

      public MessageBoxResult Confirm(string text, string title, MessageBoxButton button = MessageBoxButton.YesNo, MessageBoxImage image = MessageBoxImage.Question)
      {
         Window? owner = _resolveOwner();
         return ThemedMessageBoxView.Show(owner, text, title, button, image);
      }

      public void Info(string text, string title)
         => _ = Confirm(text, title, MessageBoxButton.OK, MessageBoxImage.Information);

      public void Warn(string text, string title)
         => _ = Confirm(text, title, MessageBoxButton.OK, MessageBoxImage.Warning);

      public void Error(string text, string title)
         => _ = Confirm(text, title, MessageBoxButton.OK, MessageBoxImage.Error);

      public string? PickBrowseFolder(string title, string defaultPath)
      {
         OpenFolderDialog dialog = new()
         {
            Title = title,
            InitialDirectory = defaultPath,
         };

         return (dialog.ShowDialog() ?? false) ? dialog.FolderName : null;
      }

      public string? PickOpenFile(string filter, string title)
      {
         OpenFileDialog dialog = new()
         {
            Title = title,
            Filter = filter,
         };

         return (dialog.ShowDialog() ?? false) ? dialog.FileName : null;
      }

      public string? PickSaveFile(string filter, string title, string? defaultFileName = null, string? initialDirectory = null)
      {
         SaveFileDialog dialog = new()
         {
            Title = title,
            Filter = filter,
            FileName = defaultFileName ?? string.Empty,
         };

         if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
         {
            dialog.InitialDirectory = initialDirectory;
         }

         return (dialog.ShowDialog() ?? false) ? dialog.FileName : null;
      }

      private static Window? _resolveOwner()
      {
         Application? app = Application.Current;
         if (app is null)
         {
            return null;
         }

         Window? active = null;
         Window? topVisible = null;

         foreach (Window window in app.Windows)
         {
            if (!window.IsLoaded || !window.IsVisible)
            {
               continue;
            }

            if (window.IsActive)
            {
               active = window;
               break;
            }

            // Prefer the most recently opened visible window over a hidden
            // MainWindow (vault session keeps MainWindow Hidden under ShowUser).
            topVisible = window;
         }

         return active ?? topVisible ?? app.MainWindow;
      }
   }
}
