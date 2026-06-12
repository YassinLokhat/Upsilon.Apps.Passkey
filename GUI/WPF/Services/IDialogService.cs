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
      /// Shows a window of type <typeparamref name="TWindow"/> modally. The optional
      /// <paramref name="configure"/> callback runs after the window is created but
      /// before <c>ShowDialog</c> is invoked.
      /// </summary>
      bool? ShowDialog<TWindow>(Action<TWindow>? configure = null) where TWindow : Window, new();

      /// <summary>
      /// Shows a window of type <typeparamref name="TWindow"/> non-modally. If a
      /// window of the same type is already open and loaded, it is activated
      /// instead of being recreated.
      /// </summary>
      TWindow Show<TWindow>(Action<TWindow>? configure = null) where TWindow : Window, new();

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
