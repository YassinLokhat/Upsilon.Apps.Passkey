using Upsilon.Apps.PassKey.Core.Public.Enums;

namespace Upsilon.Apps.PassKey.Core.Public.Events
{
   /// <summary>
   /// Represent the behavior of auto-save handling event argument.
   /// </summary>
   public class AutoSaveDetectedEventArgs : EventArgs
   {
      /// <summary>
      /// The behavior selected.
      /// By default it will merge then remove the auto-save file.
      /// </summary>
      public AutoSaveMergeBehavior MergeBehavior { get; set; } = AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile;
   }
}
