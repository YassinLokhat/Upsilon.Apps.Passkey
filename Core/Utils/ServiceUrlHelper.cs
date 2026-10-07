namespace Upsilon.Apps.Passkey.Core.Utils
{
   /// <summary>
   /// Allowlist for service URLs opened via the OS shell: absolute
   /// <c>http</c>/<c>https</c> only. Rejects <c>file:</c>, UNC, relative, and
   /// custom schemes (CWE-939 / untrusted ShellExecute).
   /// </summary>
   public static class ServiceUrlHelper
   {
      /// <summary>How a stored URL should be handled before <c>Process.Start</c>.</summary>
      public enum OpenDisposition
      {
         /// <summary>Empty, malformed, or disallowed scheme — do not open.</summary>
         Rejected,

         /// <summary>Absolute HTTPS — open without an extra prompt.</summary>
         OpenDirect,

         /// <summary>Absolute HTTP — confirm with the user before opening.</summary>
         RequiresHttpConfirmation,
      }

      /// <summary>
      /// Parses an absolute http(s) URI for storage/import. Empty/whitespace
      /// yields <see langword="null"/> and returns <see langword="true"/>.
      /// </summary>
      public static bool TryCreateAllowedUri(string? value, out Uri? uri)
      {
         uri = null;

         if (string.IsNullOrWhiteSpace(value))
         {
            return true;
         }

         if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out Uri? parsed)
            || !_isAllowedScheme(parsed))
         {
            return false;
         }

         uri = parsed;
         return true;
      }

      /// <summary>
      /// Classifies a URL for shell open: HTTPS opens directly, HTTP needs
      /// confirmation, anything else is rejected.
      /// </summary>
      public static OpenDisposition ClassifyForOpen(string? value)
      {
         if (!TryCreateAllowedUri(value, out Uri? uri) || uri is null)
         {
            return OpenDisposition.Rejected;
         }

         return uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            ? OpenDisposition.OpenDirect
            : OpenDisposition.RequiresHttpConfirmation;
      }

      /// <summary>
      /// Returns <see langword="true"/> when <paramref name="uri"/> is absolute
      /// http or https (case-insensitive scheme).
      /// </summary>
      public static bool IsAllowedScheme(Uri uri)
      {
         ArgumentNullException.ThrowIfNull(uri);
         return uri.IsAbsoluteUri && _isAllowedScheme(uri);
      }

      private static bool _isAllowedScheme(Uri uri)
         => uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
   }
}
