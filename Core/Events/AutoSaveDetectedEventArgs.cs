namespace Upsilon.Apps.PassKey.Core.Events
{
   public class AutoSaveDetectedEventArgs : EventArgs
   {
      public bool MergeAutoSave { get; set; } = true;

      public AutoSaveDetectedEventArgs() : base() { }
   }
}
