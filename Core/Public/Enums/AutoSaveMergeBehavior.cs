namespace Upsilon.Apps.PassKey.Core.Public.Enums
{
   /// <summary>
   /// Represent the behavior of auto-save handling.
   /// </summary>
   public enum AutoSaveMergeBehavior
   {
      /// <summary>
      /// The auto-save will be merged into the database and saved then the auto-save file will be removed.
      /// </summary>
      MergeAndSaveThenRemoveAutoSaveFile,
      /// <summary>
      /// The auto-save will be merged into the database without saving and the auto-save file will be keeped.
      /// </summary>
      MergeWithoutSavingAndKeepAutoSaveFile,
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
