using System;
using System.Collections.Generic;
using System.Text;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.MAUI.OSSpecific
{
    public class OSSpecificClipboardManager : IClipboardManager
    {
        public int RemoveAllOccurence(string[] removeList)
        {
            // On Windows, you could put your complex code here.
            // For now, we leave it empty so it compiles on Android/iOS/Windows
            return 0;
        }
    }
}
