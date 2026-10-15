using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Microsoft.Maui.Handlers;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Platforms.Android
{
   /// <summary>
   /// Any text field may hold vault content: autofill services must not capture
   /// it and keyboards must not learn from it. Modal pages live in their own
   /// window, so this is applied per field rather than on the activity.
   /// </summary>
   internal static class PrivateTextInput
   {
      public static void Register()
      {
         // The return-type mapping rewrites ImeOptions, so the flag is re-applied after it.
         EntryHandler.Mapper.AppendToMapping(nameof(IEntry.ReturnType), (handler, _) => _apply(handler.PlatformView));
         EditorHandler.Mapper.AppendToMapping(nameof(PrivateTextInput), (handler, _) => _apply(handler.PlatformView));
      }

      private static void _apply(EditText edit)
      {
         edit.ImportantForAutofill = ImportantForAutofill.NoExcludeDescendants;
         edit.ImeOptions = (ImeAction)((int)edit.ImeOptions | (int)ImeFlags.NoPersonalizedLearning);
      }
   }
}
