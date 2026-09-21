using System;
using System.Linq;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Helper
{
    public static class IItemHelper
    {
        public static void Shake(this IUser user)
        {
            _ = user.ItemId;
        }
    }
}