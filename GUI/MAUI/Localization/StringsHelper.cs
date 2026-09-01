using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Localization
{
   internal static class StringsHelper
   {
      public static string ComputeUserLoggedOutStrings(IActivity activity)
         => string.IsNullOrEmpty(activity.FieldValue)
            ? Strings.Format(nameof(Strings.Activity_UserLoggedOut), activity.Username)
            : Strings.Format(nameof(Strings.Activity_UserLoggedOutWithoutSaving), activity.Username);

      public static string ComputeItemUpdatedStrings(IActivity activity)
      {
         string displayValue = EnumDisplayHelper.FormatFieldValue(activity.FieldName, activity.FieldValue);

         if (!string.IsNullOrEmpty(activity.Username))
         {
            return !string.IsNullOrWhiteSpace(activity.FieldValue)
               ? Strings.Format(nameof(Strings.Activity_ItemSet), Strings.Activity_User, activity.Username, Strings.Get($"FieldName_{activity.FieldName}"), displayValue)
               : Strings.Format(nameof(Strings.Activity_ItemUpdated), Strings.Activity_User, activity.Username, Strings.Get($"FieldName_{activity.FieldName}"));
         }

         if (!string.IsNullOrEmpty(activity.ServiceName))
         {
            return !string.IsNullOrWhiteSpace(activity.FieldValue)
               ? Strings.Format(nameof(Strings.Activity_ItemSet), Strings.Label_ServiceColumn, activity.ServiceName, Strings.Get($"FieldName_{activity.FieldName}"), displayValue)
               : Strings.Format(nameof(Strings.Activity_ItemUpdated), Strings.Label_ServiceColumn, activity.ServiceName, Strings.Get($"FieldName_{activity.FieldName}"));
         }

         if (!string.IsNullOrEmpty(activity.AccountName))
         {
            return !string.IsNullOrWhiteSpace(activity.FieldValue)
               ? Strings.Format(nameof(Strings.Activity_AccountSet), activity.AccountName, Strings.Get($"FieldName_{activity.FieldName}"), activity.ParentName, displayValue)
               : Strings.Format(nameof(Strings.Activity_AccountUpdated), activity.AccountName, Strings.Get($"FieldName_{activity.FieldName}"), activity.ParentName);
         }

         throw new InvalidOperationException();
      }

      public static string ComputeItemAddedStrings(IActivity activity)
      {
         if (!string.IsNullOrEmpty(activity.Username))
         {
            return Strings.Format(nameof(Strings.Activity_ItemAdded), Strings.Activity_User, activity.Username, Strings.Label_ServiceColumn, activity.FieldValue);
         }

         if (!string.IsNullOrEmpty(activity.ServiceName))
         {
            return Strings.Format(nameof(Strings.Activity_ItemAdded), Strings.Label_ServiceColumn, activity.ServiceName, Strings.Label_AccountColumn, activity.FieldValue);
         }

         throw new InvalidOperationException();
      }

      public static string ComputeItemItemDeletedStrings(IActivity activity)
      {
         if (!string.IsNullOrEmpty(activity.Username))
         {
            return Strings.Format(nameof(Strings.Activity_ItemDeleted), Strings.Activity_User, activity.Username, Strings.Label_ServiceColumn, activity.FieldValue);
         }

         if (!string.IsNullOrEmpty(activity.ServiceName))
         {
            return Strings.Format(nameof(Strings.Activity_ItemDeleted), Strings.Label_ServiceColumn, activity.ServiceName, Strings.Label_AccountColumn, activity.FieldValue);
         }

         throw new InvalidOperationException();
      }
   }
}
