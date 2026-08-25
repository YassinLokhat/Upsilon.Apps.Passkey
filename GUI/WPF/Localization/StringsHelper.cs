using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.Localization
{
   internal static class StringsHelper
   {
      public static string ComputeUserLoggedOutStrings(IActivity activity)
         => string.IsNullOrEmpty(activity.FieldValue)
            ? Strings.Format(nameof(Strings.Activity_UserLoggedOut), activity.Username)
            : Strings.Format(nameof(Strings.Activity_UserLoggedOutWithoutSaving), activity.Username);

      public static string ComputeItemUpdatedStrings(IActivity activity)
      {
         if (!string.IsNullOrEmpty(activity.Username))
         {
            return !string.IsNullOrWhiteSpace(activity.FieldValue)
                  ? Strings.Format(nameof(Strings.Activity_ItemSet), Strings.Activity_User, activity.Username, Strings.Get($"FieldName_{activity.FieldName}"), activity.FieldValue)
                  : Strings.Format(nameof(Strings.Activity_ItemUpdated), Strings.Activity_User, activity.Username, Strings.Get($"FieldName_{activity.FieldName}"));
         }
         else if (!string.IsNullOrEmpty(activity.ServiceName))
         {
            return !string.IsNullOrWhiteSpace(activity.FieldValue)
                  ? Strings.Format(nameof(Strings.Activity_ItemSet), Strings.Activity_Service, activity.ServiceName, Strings.Get($"FieldName_{activity.FieldName}"), activity.FieldValue)
                  : Strings.Format(nameof(Strings.Activity_ItemUpdated), Strings.Activity_Service, activity.ServiceName, Strings.Get($"FieldName_{activity.FieldName}"));
         }
         else if (!string.IsNullOrEmpty(activity.AccountName))
         {
            return !string.IsNullOrWhiteSpace(activity.FieldValue)
                  ? Strings.Format(nameof(Strings.Activity_AccountSet), activity.AccountName, Strings.Get($"FieldName_{activity.FieldName}"), activity.ParentName, activity.FieldValue)
                  : Strings.Format(nameof(Strings.Activity_AccountUpdated), activity.AccountName, Strings.Get($"FieldName_{activity.FieldName}"), activity.ParentName);
         }

         throw new InvalidOperationException();
      }

      public static string ComputeItemAddedStrings(IActivity activity)
      {
         if (!string.IsNullOrEmpty(activity.Username))
         {
            return Strings.Format(nameof(Strings.Activity_ItemAdded), Strings.Activity_User, activity.Username, Strings.Activity_Service, activity.FieldValue);
         }
         else if (!string.IsNullOrEmpty(activity.ServiceName))
         {
            return Strings.Format(nameof(Strings.Activity_ItemAdded), Strings.Activity_Service, activity.ServiceName, Strings.Activity_Account, activity.FieldValue);
         }

         throw new InvalidOperationException();
      }

      public static string ComputeItemItemDeletedStrings(IActivity activity)
      {
         if (!string.IsNullOrEmpty(activity.Username))
         {
            return Strings.Format(nameof(Strings.Activity_ItemDeleted), Strings.Activity_User, activity.Username, Strings.Activity_Service, activity.FieldValue);
         }
         else if (!string.IsNullOrEmpty(activity.ServiceName))
         {
            return Strings.Format(nameof(Strings.Activity_ItemDeleted), Strings.Activity_Service, activity.ServiceName, Strings.Activity_Account, activity.FieldValue);
         }

         throw new InvalidOperationException();
      }
   }
}
