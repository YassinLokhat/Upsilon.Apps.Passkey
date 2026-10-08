namespace Upsilon.Apps.Passkey.Utils.LeakFilter
{
   /// <summary>
   /// The on-disk <c>.pkbf</c> failed format checks and cannot be refreshed in place.
   /// Callers must use <see cref="HibpBloomBuildMode.Rebuild"/> (or a first build)
   /// rather than treating this as a soft skip.
   /// </summary>
   /// <remarks>
   /// Derives from <see cref="Exception"/> because <see cref="InvalidDataException"/>
   /// is sealed on modern .NET. Hosts that soft-catch transport/disk failures must
   /// list this type explicitly. The wrap still preserves the original
   /// <see cref="InvalidDataException"/> as <see cref="Exception.InnerException"/>.
   /// </remarks>
   public sealed class HibpBloomCorruptException : Exception
   {
      public HibpBloomCorruptException()
      {
      }

      public HibpBloomCorruptException(string message)
         : base(message)
      {
      }

      public HibpBloomCorruptException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }
}
