using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;

namespace Upsilon.Apps.Passkey.GUI.WPF.Adapters
{
   /// <summary>
   /// Reads a <see cref="PasswordBox"/> through its <see cref="SecureString"/>; the
   /// unmanaged BSTR copy is zeroed before <see cref="Read"/> returns.
   /// </summary>
   internal sealed class PasswordBoxSecretInput(PasswordBox box) : ISecretInput
   {
      private readonly PasswordBox _box = box ?? throw new ArgumentNullException(nameof(box));

      public string Read()
      {
         using SecureString secure = _box.SecurePassword;
         if (secure.Length == 0)
         {
            return string.Empty;
         }

         IntPtr bstr = IntPtr.Zero;
         try
         {
            bstr = Marshal.SecureStringToBSTR(secure);
            return Marshal.PtrToStringBSTR(bstr);
         }
         finally
         {
            if (bstr != IntPtr.Zero)
            {
               Marshal.ZeroFreeBSTR(bstr);
            }
         }
      }

      public void Write(string secret) => _box.Password = secret;

      public void Clear() => _box.Clear();
   }
}
