using System.Windows;

namespace Upsilon.Apps.Passkey.GUI.WPF.Services
{
   /// <summary>
   /// Abstracts every window/message-box/file-dialog open call so view-models
   /// can stay free of WPF dependencies and remain testable.
   /// </summary>
   public interface IDialogService
   {
      /// <summary>
      /// Shows the supplied <paramref name="window"/> modally as a child of the
      /// currently active window.
      /// </summary>
      bool? ShowDialog<TWindow>(TWindow window) where TWindow : Window;

      /// <summary>
      /// Shows a window of type <typeparamref name="TWindow"/> non-modally. If a
      /// window of the same type is already open and loaded, it is activated
      /// (after running <paramref name="configure"/>) instead of being recreated.
      /// </summary>
      TWindow ShowSingleton<TWindow>(Func<TWindow> factory, Action<TWindow>? configure = null) where TWindow : Window;

      /// <summary>Returns the singleton window of type <typeparamref name="TWindow"/> currently being tracked, if any.</summary>
      TWindow? GetSingleton<TWindow>() where TWindow : Window;

      /// <summary>Closes the singleton window of type <typeparamref name="TWindow"/> if any.</summary>
      void Close<TWindow>() where TWindow : Window;

      /// <summary>Shows a confirmation dialog with the given prompt.</summary>
      MessageBoxResult Confirm(string text, string title, MessageBoxButton button = MessageBoxButton.YesNo, MessageBoxImage image = MessageBoxImage.Question);

      /// <summary>Shows an information dialog with the given message.</summary>
      void Info(string text, string title);

      /// <summary>Shows a warning dialog with the given message.</summary>
      void Warn(string text, string title);

      /// <summary>Asks the user to pick an existing file. Returns <c>null</c> when cancelled.</summary>
      string? PickOpenFile(string filter, string title);

      /// <summary>Asks the user to pick a destination file. Returns <c>null</c> when cancelled.</summary>
      string? PickSaveFile(string filter, string title, string? defaultFileName = null);
   }
}
