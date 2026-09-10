namespace Upsilon.Apps.Passkey.Interfaces.Enums
{
   /// <summary>
   /// Intrinsic urgency of an alert. Consumers map this to presentation
   /// (e.g. brushes); they must not invent severity from <see cref="Models.IAlert.Kind"/>.
   /// </summary>
   public enum AlertSeverity
   {
      Info = 0,
      Warning = 1,
      Critical = 2,
   }
}
