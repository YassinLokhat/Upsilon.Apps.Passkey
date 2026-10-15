namespace Upsilon.Apps.Passkey.GUI.Presentation.Models
{
   /// <summary>
   /// Platform-neutral field chrome for dirty / weak / leaked secret fields.
   /// Hosts map to brushes or styles.
   /// </summary>
   internal enum FieldVisualState
   {
      Default = 0,
      Dirty = 1,
      Weak = 2,
      Leaked = 3,
      WeakAndLeaked = 4,
   }

   internal static class FieldVisualStateHelper
   {
      public static FieldVisualState FromFlags(bool isDirty, bool isNotifiedWeak, bool isNotifiedLeak)
      {
         if (isNotifiedWeak && isNotifiedLeak)
         {
            return FieldVisualState.WeakAndLeaked;
         }

         if (isNotifiedLeak)
         {
            return FieldVisualState.Leaked;
         }

         if (isNotifiedWeak)
         {
            return FieldVisualState.Weak;
         }

         return isDirty ? FieldVisualState.Dirty : FieldVisualState.Default;
      }
   }
}
