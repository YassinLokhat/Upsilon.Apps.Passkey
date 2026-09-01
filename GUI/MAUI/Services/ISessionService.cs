
namespace Upsilon.Apps.Passkey.GUI.MAUI.Services
{
   internal interface ISessionService
   {
      IDatabase? Database { get; }

      IUser? User { get; }

      event EventHandler? SessionChanged;

      void StartSession(IDatabase database);

      void EndSession(bool closeDatabase = true);

      void ApplySessionLanguage();

      void ApplySessionTheme();
   }
}
