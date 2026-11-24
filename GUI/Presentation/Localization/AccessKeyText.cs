using System.Text;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Localization
{
   /// <summary>
   /// Menu strings mark their WPF access key with <c>_</c> (<c>__</c> is a literal
   /// underscore). Hosts without access keys display them through <see cref="Strip"/>.
   /// </summary>
   internal static class AccessKeyText
   {
      public static string Strip(string text)
      {
         ArgumentNullException.ThrowIfNull(text);
         if (!text.Contains('_', StringComparison.Ordinal))
         {
            return text;
         }

         StringBuilder builder = new(text.Length);
         int index = 0;
         while (index < text.Length)
         {
            char current = text[index];
            if (current != '_')
            {
               _ = builder.Append(current);
               index++;
            }
            else if (index + 1 < text.Length && text[index + 1] == '_')
            {
               _ = builder.Append('_');
               index += 2;
            }
            else
            {
               index++;
            }
         }

         return builder.ToString();
      }
   }
}
