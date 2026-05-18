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
            // Sur Windows, vous pourriez mettre votre code complexe.
            // Pour l'instant, on laisse vide pour que ça compile sur Android/iOS/Windows.
            return 0;
        }
    }
}
