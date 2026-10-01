using Microsoft.Win32;
using System.IO;
using System.Windows;
using Upsilon.Apps.Passkey.GUI.WPF.Views;

namespace Upsilon.Apps.Passkey.GUI.WPF.Services
{
   internal sealed class DialogService : IDialogService
   {
      private readonly Dictionary<Type, Window> _singletons = [];

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
