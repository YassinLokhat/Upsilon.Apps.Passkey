namespace Upsilon.Apps.Passkey.Interfaces.Enums
{
   /// <summary>
   /// Represent the behavior of auto-save handling.
   /// </summary>
   public enum AutoSaveMergeBehavior
   {
      /// <summary>
      /// The behavior of auto-save handling is undefined.
      /// </summary>
      Undefined = 0,
      /// <summary>
      /// The auto-save will be merged into the database and saved then the auto-save file will be removed.
      /// </summary>
      MergeAndSaveThenRemoveAutoSaveFile,
      /// <summary>
      /// The auto-save will be merged into the database without saving and the auto-save file will be kept.
      /// </summary>
      MergeWithoutSavingAndKeepAutoSaveFile,
      /// <summary>
      /// The auto-save will not be merged into the database but the auto-save file will be removed.
      /// </summary>
      DontMergeAndRemoveAutoSaveFile,
      /// <summary>
      /// The auto-save will not be merged into the database and the auto-save file will be kept.
      /// </summary>
      DontMergeAndKeepAutoSaveFile,
   }
}
