using System.Windows;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Services;

namespace Upsilon.Apps.Passkey.UnitTests.Windows.Gui.Fakes
{
   /// <summary>
   /// Records dialog calls without showing UI.
   /// </summary>
   internal sealed class FakeDialogService : IWindowDialogService, IDialogService
   {
      public List<string> Confirms { get; } = [];

      public Task<DialogResult> ConfirmAsync(string text, string title, DialogButtons buttons = DialogButtons.YesNo, DialogIcon icon = DialogIcon.Question, CancellationToken cancellationToken = default)
         => Task.FromResult(DialogService.ToDialogResult(Confirm(text, title)));

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
         => Task.FromResult(BrowseFolderResult);

      public Task<string?> PickOpenFileAsync(string filter, string title, CancellationToken cancellationToken = default)
         => Task.FromResult(OpenFileResult);

      public Task<string?> PickSaveFileAsync(string filter, string title, string? defaultFileName = null, string? initialDirectory = null, CancellationToken cancellationToken = default)
         => Task.FromResult(SaveFileResult);

      public Task<TResult?> ShowAsync<TResult>(IViewRequest<TResult> request, CancellationToken cancellationToken = default)
         => Task.FromResult<TResult?>(default);

      public List<string> Warnings { get; } = [];

      public List<string> Infos { get; } = [];

      public List<string> Errors { get; } = [];

      public MessageBoxResult ConfirmResult { get; set; } = MessageBoxResult.Yes;

      public string? BrowseFolderResult { get; set; }
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
      {
         Confirms.Add(text);
         return ConfirmResult;
      }

      public void Info(string text, string title) => Infos.Add(text);

      public void Warn(string text, string title) => Warnings.Add(text);

      public void Error(string text, string title) => Errors.Add(text);

      public string? PickBrowseFolder(string title, string defaultPath) => BrowseFolderResult;

      public string? PickOpenFile(string filter, string title) => OpenFileResult;

      public string? PickSaveFile(string filter, string title, string? defaultFileName = null, string? initialDirectory = null) => SaveFileResult;
   }
}
