using System.Windows;
using Upsilon.Apps.Passkey.GUI.WPF.Services;

namespace Upsilon.Apps.Passkey.UnitTests.Gui.Fakes
{
   /// <summary>
   /// Records dialog calls without showing UI.
   /// </summary>
   internal sealed class FakeDialogService : IDialogService
   {
      public List<string> Warnings { get; } = [];

      public List<string> Infos { get; } = [];

      public MessageBoxResult ConfirmResult { get; set; } = MessageBoxResult.Yes;

      public string? OpenFileResult { get; set; }

      public string? SaveFileResult { get; set; }

      public bool? ShowDialog<TWindow>(TWindow window) where TWindow : Window => true;

      public TWindow ShowSingleton<TWindow>(Func<TWindow> factory, Action<TWindow>? configure = null) where TWindow : Window
      {
         TWindow window = factory();
         configure?.Invoke(window);
         return window;
      }

      public TWindow? GetSingleton<TWindow>() where TWindow : Window => null;

      public void Close<TWindow>() where TWindow : Window
      {
      }

      public MessageBoxResult Confirm(string text, string title, MessageBoxButton button = MessageBoxButton.YesNo, MessageBoxImage image = MessageBoxImage.Question)
         => ConfirmResult;

      public void Info(string text, string title) => Infos.Add(text);

      public void Warn(string text, string title) => Warnings.Add(text);

      public string? PickOpenFile(string filter, string title) => OpenFileResult;

      public string? PickSaveFile(string filter, string title, string? defaultFileName = null) => SaveFileResult;
   }
}
