namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Small string helpers for WPF display text.
   /// </summary>
   internal static class TextHelper
   {
      /// <summary>
      /// Uppercases the first character of <paramref name="text"/> so activity
      /// messages start with a capital letter regardless of UI language.
      /// </summary>
      public static string ToSentenceCase(string text)
         => string.IsNullOrEmpty(text) ? text : text[..1].ToUpperInvariant() + text[1..];
   }
}
