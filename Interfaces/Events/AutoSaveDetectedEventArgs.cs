using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Events
{
   /// <summary>
   /// Raised when an autosave entry is found at login. Set
   /// <see cref="MergeBehavior"/> before returning from the handler.
   /// </summary>
   public class AutoSaveDetectedEventArgs : EventArgs
   {
      /// <summary>
      /// Defaults to merge, save, then remove the autosave entry.
      /// </summary>
      public AutoSaveMergeBehavior MergeBehavior { get; set; } = AutoSaveMergeBehavior.MergeAndSaveThenRemoveAutoSaveFile;
   }
}
