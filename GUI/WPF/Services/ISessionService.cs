using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.Services
{
   /// <summary>
   /// Tracks the currently open <see cref="IDatabase"/> for the whole UI.
   /// Replaces the static <c>MainViewModel.Database</c> field so consumers can
   /// be unit-tested and react to lifecycle changes through events.
   /// </summary>
   public interface ISessionService
   {
      /// <summary>The active database, or <c>null</c> when nobody is logged in.</summary>
      IDatabase? Database { get; }

      /// <summary>The active user, or <c>null</c> when no database is loaded or the user is not logged in.</summary>
      IUser? User { get; }

      /// <summary>Raised whenever <see cref="Database"/> or <see cref="User"/> changes.</summary>
      event EventHandler? SessionChanged;

      /// <summary>
      /// Registers <paramref name="database"/> as the current session. Any existing
      /// session is closed first.
      /// </summary>
      void StartSession(IDatabase database);

      /// <summary>
      /// Closes the current session, if any.
      /// </summary>
      void EndSession();
   }
}
