using Upsilon.Apps.PassKey.Core.Enums;

namespace Upsilon.Apps.PassKey.Core.Events
{
   public class AutoSaveDetectedEventArgs : EventArgs
   {
      public AutoSaveMergeBehavior MergeBehavior { get; set; } = AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile;

      public AutoSaveDetectedEventArgs() : base() { }
   }
}
