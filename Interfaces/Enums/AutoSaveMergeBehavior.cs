namespace Upsilon.Apps.Passkey.Interfaces.Enums
{
   /// <summary>
   /// How to handle an autosave entry discovered at login.
   /// </summary>
   public enum AutoSaveMergeBehavior
   {
      Undefined = 0,
      MergeAndSaveThenRemoveAutoSaveFile,
      MergeWithoutSavingAndKeepAutoSaveFile,
      DontMergeAndRemoveAutoSaveFile,
      DontMergeAndKeepAutoSaveFile,
   }
}
