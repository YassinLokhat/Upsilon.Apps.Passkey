using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Models
{
   /// <summary>Opens the password generator; the result is the inserted password, or <c>null</c>.</summary>
   internal sealed record GeneratePasswordRequest : IViewRequest<string>;

   /// <summary>Picks or types an identifier among <paramref name="Identifiers"/>.</summary>
   internal sealed record InsertIdentifierRequest(IReadOnlyList<IIdentifier> Identifiers, IIdentifier? Current)
      : IViewRequest<IIdentifier>;

   /// <summary>Shows <paramref name="Content"/> as a QR code for <paramref name="DelayMilliseconds"/> (0 = until closed).</summary>
   internal sealed record ShowQrCodeRequest(string Content, int DelayMilliseconds) : IViewRequest<bool>
   {
      public override string ToString() => nameof(ShowQrCodeRequest);
   }

   /// <summary>Progressive credential re-entry; <c>true</c> once every factor matched in order.</summary>
   internal sealed record ConfirmCredentialsRequest(IReadOnlyList<string> Credentials, bool IsNew) : IViewRequest<bool>
   {
      public override string ToString() => nameof(ConfirmCredentialsRequest);
   }
}
