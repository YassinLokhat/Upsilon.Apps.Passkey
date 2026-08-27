using Microsoft.Win32;
using System.Windows;

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
         return owner is null
            ? MessageBox.Show(text, title, button, image)
            : MessageBox.Show(owner, text, title, button, image);
      }

      public void Info(string text, string title)
         => _ = Confirm(text, title, MessageBoxButton.OK, MessageBoxImage.Information);

      public void Warn(string text, string title)
         => _ = Confirm(text, title, MessageBoxButton.OK, MessageBoxImage.Warning);

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

      public string? PickSaveFile(string filter, string title, string? defaultFileName = null)
      {
         SaveFileDialog dialog = new()
         {
            Title = title,
            Filter = filter,
            FileName = defaultFileName ?? string.Empty,
         };

         return (dialog.ShowDialog() ?? false) ? dialog.FileName : null;
      }

      private static Window? _resolveOwner()
      {
         Window? application = Application.Current?.MainWindow;
         if (application is null)
         {
            return null;
         }

         foreach (Window window in Application.Current!.Windows)
         {
            if (window.IsActive)
            {
               return window;
            }
         }

         return application;
      }
   }
}
