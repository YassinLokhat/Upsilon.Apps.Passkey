namespace Upsilon.Apps.Passkey.GUI.Presentation.Ports
{
   /// <summary>
   /// Host diagnostic log. Messages must never contain secrets (passwords,
   /// passkeys, identifiers, notes) nor exception messages that may embed them.
   /// </summary>
   internal interface IAppLog
   {
      void Info(string message);

      void Warn(string message);

      void Error(string message, Exception? exception = null);
   }
}
