namespace Upsilon.Apps.Passkey.GUI.Presentation.Ports
{
   /// <summary>
   /// A user-picked document for import or export. <see cref="Extension"/> keeps its leading dot
   /// and original casing (".json", ".CSV") and decides the plaintext format.
   /// </summary>
   internal sealed class ExchangeFile(Stream stream, string extension, string displayName) : IAsyncDisposable, IDisposable
   {
      public Stream Stream { get; } = stream ?? throw new ArgumentNullException(nameof(stream));

      public string Extension { get; } = extension ?? string.Empty;

      public string DisplayName { get; } = displayName ?? string.Empty;

      public ValueTask DisposeAsync() => Stream.DisposeAsync();

      public void Dispose() => Stream.Dispose();
   }

   /// <summary>
   /// Stream-based file picking so hosts without file-system paths (Android SAF) can
   /// import and export without staging plaintext in temporary files.
   /// </summary>
   internal interface IFileExchange
   {
      /// <summary>Returns <see langword="null"/> when the user cancels.</summary>
      Task<ExchangeFile?> OpenReadAsync(string title, IReadOnlyList<string> extensions, CancellationToken cancellationToken = default);

      /// <summary>
      /// Returns a writable, empty stream, or <see langword="null"/> when the user cancels
      /// or declines to replace an existing document.
      /// </summary>
      Task<ExchangeFile?> OpenWriteAsync(string title, string suggestedFileName, string extension, CancellationToken cancellationToken = default);
   }
}
