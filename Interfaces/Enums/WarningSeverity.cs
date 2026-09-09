namespace Upsilon.Apps.Passkey.Interfaces.Enums
{
   /// <summary>
   /// Intrinsic urgency of a warning. Consumers map this to presentation
   /// (e.g. brushes); they must not invent severity from <see cref="Models.IWarning.Kind"/>.
   /// </summary>
   public enum WarningSeverity
   {
      Info = 0,
      Warning = 1,
      Critical = 2,
   }
}
