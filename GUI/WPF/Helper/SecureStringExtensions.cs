using System.Runtime.InteropServices;
using System.Security;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Helpers that bridge between WPF's <see cref="SecureString"/> (used by
   /// <c>PasswordBox.SecurePassword</c>) and the rest of the API surface, while
   /// keeping the unmanaged BSTR copy alive only for the duration of the
   /// supplied callback and zero-ing it out afterwards.
   /// </summary>
   internal static class SecureStringExtensions
   {
      /// <summary>
      /// Pins <paramref name="value"/> as an unmanaged BSTR, invokes
      /// <paramref name="action"/> with the resulting (managed) string and then
      /// zeros the unmanaged buffer. The managed string returned to the caller
      /// still lives in the heap until the GC collects it; callers should keep
      /// their use of it as short as possible.
      /// </summary>
      public static T UseAsString<T>(this SecureString value, Func<string, T> action)
      {
         ArgumentNullException.ThrowIfNull(value);
         ArgumentNullException.ThrowIfNull(action);

         IntPtr bstr = IntPtr.Zero;

         try
         {
            bstr = Marshal.SecureStringToBSTR(value);
            string managed = Marshal.PtrToStringBSTR(bstr);
            return action(managed);
         }
         finally
         {
            if (bstr != IntPtr.Zero)
            {
               Marshal.ZeroFreeBSTR(bstr);
            }
         }
      }

      /// <summary>
      /// Same as <see cref="UseAsString{T}"/> but with an <see cref="Action{T}"/>
      /// callback when no value needs to be returned.
      /// </summary>
      public static void UseAsString(this SecureString value, Action<string> action)
      {
         _ = value.UseAsString(s =>
         {
            action(s);
            return 0;
         });
      }
   }
}
