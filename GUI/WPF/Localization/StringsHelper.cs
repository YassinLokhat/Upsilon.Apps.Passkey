using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.Localization
{
   internal static class StringsHelper
   {
      public static string ComputeUserLoggedOutStrings(string? fieldValue, string? itemName)
         => string.IsNullOrEmpty(fieldValue)
            ? Strings.Format(nameof(Strings.Activity_UserLoggedOut), itemName)
            : Strings.Format(nameof(Strings.Activity_UserLoggedOutWithoutSaving), itemName);

      public static string ComputeItemUpdatedStrings(string? itemName, string? fieldName, string? fieldValue, string? parentName)
         => string.IsNullOrWhiteSpace(fieldValue)
            ? string.IsNullOrEmpty(parentName)
               ? Strings.Format(nameof(Strings.Activity_ItemUpdated), itemName, Strings.Get($"FieldName_{fieldName}"))
               : Strings.Format(nameof(Strings.Activity_AccountUpdated), itemName, Strings.Get($"FieldName_{fieldName}"), parentName)
            : string.IsNullOrEmpty(parentName)
               ? Strings.Format(nameof(Strings.Activity_ItemSet), itemName, Strings.Get($"FieldName_{fieldName}"), fieldValue)
               : Strings.Format(nameof(Strings.Activity_AccountSet), itemName, Strings.Get($"FieldName_{fieldName}"), parentName, fieldValue);
   }
}
