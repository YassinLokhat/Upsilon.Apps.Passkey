using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   /// <summary>
   /// Represent a warning.
   /// </summary>
   public interface IWarning
   {
      /// <summary>
      /// The type of the warning.
      /// </summary>
      WarningType WarningType { get; }

      /// <summary>
      /// The activities concerned to the warning, if exists.
      /// </summary>
      IActivity[]? Activities { get; }

      /// <summary>
      /// The accounts concerned to the warning, if exists.
      /// </summary>
      IAccount[]? Accounts { get; }
   }
}
