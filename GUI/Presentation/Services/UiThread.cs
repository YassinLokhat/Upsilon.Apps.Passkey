namespace Upsilon.Apps.Passkey.GUI.Presentation.Services
{
   /// <summary>
   /// Marshals work through <see cref="AppServices.Ui"/>. Runs inline when no
   /// dispatcher is configured (unit tests) or when already on the UI thread.
   /// </summary>
   internal static class UiThread
   {
      public static void Post(Action action)
      {
         ArgumentNullException.ThrowIfNull(action);

         if (AppServices.Ui is { } ui && !ui.CheckAccess())
         {
            ui.Post(action);
            return;
         }

         action();
      }

      public static void Send(Action action)
      {
         ArgumentNullException.ThrowIfNull(action);

         if (AppServices.Ui is { } ui && !ui.CheckAccess())
         {
            ui.Send(action);
            return;
         }

         action();
      }
   }
}
