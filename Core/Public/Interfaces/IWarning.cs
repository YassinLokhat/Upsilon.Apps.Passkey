using Upsilon.Apps.Passkey.Core.Public.Enums;

namespace Upsilon.Apps.Passkey.Core.Public.Interfaces
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
      /// The logs concerned to the warning, if exists.
      /// </summary>
      ILog[]? Logs { get; }

      /// <summary>
      /// The accounts concerned to the warning, if exists.
      /// </summary>
      IAccount[]? Accounts { get; }
   }
}
