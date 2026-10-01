namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   /// <summary>
   /// Session-scoped undo/redo stack for vault edits. Cleared on
   /// <see cref="IDatabase.Close"/>; never persisted to the <c>.pku</c>.
   /// </summary>
   public interface IEditHistory
   {
      bool CanUndo { get; }

      bool CanRedo { get; }

      void Undo();

      void Redo();

      /// <summary>
      /// Drops undo and redo stacks. Called automatically when the vault session ends.
      /// </summary>
      void Clear();

      /// <summary>
      /// Raised after <see cref="Undo"/>, <see cref="Redo"/>, <see cref="Clear"/>,
      /// or when a new user edit is recorded.
      /// </summary>
      event EventHandler? HistoryChanged;
   }
}
