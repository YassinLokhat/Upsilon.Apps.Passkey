using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   public interface IInsufficientPasskeysAlert : IAlert
   {
      int Count { get; }

      int RecommendedMinimum { get; }
   }

   public interface IWeakPasskeyAlert : IAlert
   {
      /// <summary>Zero-based onion layer indices that failed the quality check.</summary>
      IReadOnlyList<int> PasskeyIndexes { get; }

      SecretQualityIssue Issues { get; }
   }

   public interface IPasskeyLeakedAlert : IAlert
   {
      /// <summary>Zero-based onion layer indices found in a leak corpus.</summary>
      IReadOnlyList<int> PasskeyIndexes { get; }
   }
}
