using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Localization
{
   /// <summary>
   /// Fixed, localized reasons for user-facing errors. Exception messages may
   /// embed paths, SAF URIs or account names, so they are never displayed.
   /// </summary>
   internal static class ErrorText
   {
      public static string Describe(Exception exception)
      {
         ArgumentNullException.ThrowIfNull(exception);

         return exception switch
         {
            UnauthorizedAccessException => Strings.Error_AccessDenied,
            CorruptedSourceException or NullValueException => Strings.Error_InvalidVault,
            IOException => Strings.Error_FileAccess,
            _ => Strings.Error_Unexpected,
         };
      }

      /// <summary>Log-safe description: exception type and HResult only.</summary>
      public static string ForLog(Exception exception)
      {
         ArgumentNullException.ThrowIfNull(exception);
         return $"{exception.GetType().Name} (0x{exception.HResult:X8})";
      }
   }
}
