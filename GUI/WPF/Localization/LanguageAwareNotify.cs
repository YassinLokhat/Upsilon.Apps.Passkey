using System.Diagnostics.CodeAnalysis;
using System.Windows;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;

namespace Upsilon.Apps.Passkey.GUI.WPF.Localization
{
   /// <summary>
   /// Single entry point used by <see cref="LocalizationService"/> after a culture
   /// change. Every dialog should implement <see cref="ILanguageAware"/> on the
   /// <see cref="Window"/> and forward to its DataContext (plus any code-behind
   /// snapshots). Failures are isolated so one window cannot skip the rest.
   /// </summary>
   internal static class LanguageAwareNotify
   {
      /// <summary>
      /// Refreshes culture-dependent snapshots on <paramref name="window"/>.
      /// When the window implements <see cref="ILanguageAware"/>, only the window
      /// handler runs (it must forward to DataContext). Otherwise the DataContext
      /// is notified as a legacy fallback.
      /// </summary>
      [SuppressMessage(
         "Design",
         "CA1031:Do not catch general exception types",
         Justification = "A failing OnLanguageChanged must not abort refresh of other open windows; the exception is logged.")]
      public static void Notify(Window window)
      {
         ArgumentNullException.ThrowIfNull(window);

         try
         {
            if (window is ILanguageAware windowAware)
            {
               windowAware.OnLanguageChanged();
               return;
            }

            if (window.DataContext is ILanguageAware dataContextAware)
            {
               dataContextAware.OnLanguageChanged();
            }
         }
         catch (Exception ex)
         {
            Log.Error(ex, $"Language refresh failed for {window.GetType().Name}");
         }
      }

      /// <summary>
      /// Forwards to <c>(DataContext as ILanguageAware)?.OnLanguageChanged()</c>.
      /// Call from window <see cref="ILanguageAware.OnLanguageChanged"/> handlers.
      /// </summary>
      public static void ForwardToDataContext(this Window window)
      {
         if (window.DataContext is ILanguageAware dataContextAware)
         {
            dataContextAware.OnLanguageChanged();
         }
      }
   }
}
