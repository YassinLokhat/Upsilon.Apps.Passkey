using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Upsilon.Apps.Passkey.GUI.Helper
{
   public static class PropertyHelper
   {
      public static bool SetProperty<T>(ref T field,
         T newValue,
         object sender,
         PropertyChangedEventHandler? PropertyChanged,
         [CallerMemberName] string? propertyName = null)
      {
         if (!Equals(field, newValue))
         {
            field = newValue;
            PropertyChanged?.Invoke(sender, new PropertyChangedEventArgs(propertyName));

            return true;
         }

         return false;
      }
   }
}
