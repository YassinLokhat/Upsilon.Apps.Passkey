using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   public interface IInsufficientPasskeysWarning : IWarning
   {
      int Count { get; }

      int RecommendedMinimum { get; }
   }

   public interface IWeakPasskeyWarning : IWarning
   {
      /// <summary>Zero-based onion layer indices that failed the quality check.</summary>
      IReadOnlyList<int> PasskeyIndexes { get; }

      SecretQualityIssue Issues { get; }
   }

   public interface IPasskeyLeakedWarning : IWarning
   {
      /// <summary>Zero-based onion layer indices found in a leak corpus.</summary>
      IReadOnlyList<int> PasskeyIndexes { get; }
   }
}
