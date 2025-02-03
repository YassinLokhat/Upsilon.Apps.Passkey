namespace Upsilon.Apps.PassKey.Core.Abstraction.Enums
{
   /// <summary>
   /// Represent the behavior of auto-save handling.
   /// </summary>
   public enum AutoSaveMergeBehavior
   {
      /// <summary>
      /// The auto-save will be merged into the database then the auto-save file will be removed.
      /// </summary>
      MergeThenRemoveAutoSaveFile,
      /// <summary>
      /// The auto-save will not be merged into the database but the auto-save file will be removed.
      /// </summary>
      DontMergeAndRemoveAutoSaveFile,
      /// <summary>
      /// The auto-save will not be merged into the database and the auto-save file will be keeped.
      /// </summary>
      DontMergeAndKeepAutoSaveFile,
   }
}
