using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;

namespace UnitTests.Presentation.Fakes
{
   /// <summary>Runs every marshalled call inline and reports "not on the UI thread".</summary>
   internal sealed class InlineUiDispatcher : IUiDispatcher
   {
      public bool OnUiThread { get; set; }

      public void Post(Action action) => action();

      public void Send(Action action) => action();

      public T Send<T>(Func<T> func) => func();

      public bool CheckAccess() => OnUiThread;
   }

   internal sealed class FakeDialogService : IDialogService
   {
      public DialogResult ConfirmAnswer { get; set; } = DialogResult.Cancel;

      public int ConfirmCount { get; private set; }

      public Task<DialogResult> ConfirmAsync(string text, string title, DialogButtons buttons = DialogButtons.YesNo, DialogIcon icon = DialogIcon.Question, CancellationToken cancellationToken = default)
      {
         ConfirmCount++;
         return Task.FromResult(ConfirmAnswer);
      }

      public Task InfoAsync(string text, string title, CancellationToken cancellationToken = default) => Task.CompletedTask;

      public Task WarnAsync(string text, string title, CancellationToken cancellationToken = default) => Task.CompletedTask;

      public Task ErrorAsync(string text, string title, CancellationToken cancellationToken = default) => Task.CompletedTask;

      public Task<string?> PickBrowseFolderAsync(string title, string defaultPath, CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);

      public Task<string?> PickOpenFileAsync(string filter, string title, CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);

      public Task<string?> PickSaveFileAsync(string filter, string title, string? defaultFileName = null, string? initialDirectory = null, CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);

      public List<object> ShownRequests { get; } = [];

      /// <summary>Result returned for the next requests, keyed by request type.</summary>
      public Dictionary<Type, object?> ViewResults { get; } = [];

      public Task<TResult?> ShowAsync<TResult>(IViewRequest<TResult> request, CancellationToken cancellationToken = default)
      {
         ShownRequests.Add(request);
         return Task.FromResult(ViewResults.TryGetValue(request.GetType(), out object? result) ? (TResult?)result : default);
      }
   }

   internal sealed class FakeAppPaths(string defaultDatabaseDirectory) : IAppPaths
   {
      public string ConfigDirectory => defaultDatabaseDirectory;

      public string ConfigFilePath => Path.Join(defaultDatabaseDirectory, "config.json");

      public string DefaultDatabaseDirectory { get; } = defaultDatabaseDirectory;

      public string LogDirectory => defaultDatabaseDirectory;

      public string? OfflineLeakFilterPath { get; init; }

      public string CacheDirectory => defaultDatabaseDirectory;
   }

   internal sealed class FakeClipboardService : IClipboardService
   {
      public event EventHandler<ClipboardSecretCopiedEventArgs>? SecretCopied;

      public void RaiseSecretCopied(string text) => SecretCopied?.Invoke(this, new ClipboardSecretCopiedEventArgs(text));

      public void SetText(string text, TimeSpan? autoClearAfter = null)
      {
      }

      public void SetText(string text, int autoClearAfter)
      {
      }

      public Task<int> RemoveAllOccurrenceAsync(IEnumerable<string> removeList, CancellationToken cancellationToken = default)
         => Task.FromResult(0);

      public void ClearIfStillOwned()
      {
      }
   }
}
