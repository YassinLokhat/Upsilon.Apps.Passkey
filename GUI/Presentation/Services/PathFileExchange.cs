using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Services
{
   /// <summary>
   /// <see cref="IFileExchange"/> over the host's path pickers. The save picker is
   /// responsible for the overwrite prompt.
   /// </summary>
   internal sealed class PathFileExchange(IDialogService dialogs) : IFileExchange
   {
      private readonly IDialogService _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));

      public async Task<ExchangeFile?> OpenReadAsync(string title, IReadOnlyList<string> extensions, CancellationToken cancellationToken = default)
      {
         ArgumentNullException.ThrowIfNull(extensions);
         string filter = string.Join('|', extensions.Select(e => _filterFor(e, export: false)));
         string? path = await _dialogs.PickOpenFileAsync(filter, title, cancellationToken).ConfigureAwait(true);
         if (string.IsNullOrEmpty(path))
         {
            return null;
         }

         return _open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
      }

      public async Task<ExchangeFile?> OpenWriteAsync(string title, string suggestedFileName, string extension, CancellationToken cancellationToken = default)
      {
         string? path = await _dialogs.PickSaveFileAsync(_filterFor(extension, export: true), title, suggestedFileName, cancellationToken: cancellationToken).ConfigureAwait(true);
         if (string.IsNullOrEmpty(path))
         {
            return null;
         }

         return _open(path, FileMode.Create, FileAccess.Write, FileShare.None);
      }

      private static ExchangeFile _open(string path, FileMode mode, FileAccess access, FileShare share)
      {
         FileStream? stream = null;
         try
         {
            stream = new FileStream(path, mode, access, share);
            ExchangeFile file = new(stream, Path.GetExtension(path), Path.GetFileName(path));
            stream = null;
            return file;
         }
         finally
         {
            stream?.Dispose();
         }
      }

      private static string _filterFor(string extension, bool export)
         => extension.ToUpperInvariant() switch
         {
            ".JSON" => Strings.Filter_Json,
            ".CSV" => export ? Strings.Filter_CsvExport : Strings.Filter_Csv,
            _ => throw new ArgumentOutOfRangeException(nameof(extension), extension, null),
         };
   }
}
