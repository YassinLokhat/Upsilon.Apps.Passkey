using System.Globalization;
using System.Resources;

namespace Upsilon.Apps.Passkey.GUI.WPF.Localization
{
   /// <summary>
   /// Strongly-named accessors over <c>Strings.resx</c> / satellite cultures.
   /// Add a language by copying <c>Strings.resx</c> to <c>Strings.xx.resx</c>
   /// and registering it in <see cref="LocalizationService.Shipped"/>.
   /// </summary>
   internal static class Strings
   {
      private static readonly ResourceManager _manager = new(
         "Upsilon.Apps.Passkey.GUI.WPF.Localization.Strings",
         typeof(Strings).Assembly);

      public static string Get(string name)
         => _manager.GetString(name, CultureInfo.CurrentUICulture) ?? name;

      public static string GetNeutral(string name)
         => _manager.GetString(name, CultureInfo.InvariantCulture) ?? name;

      public static string Format(string name, params object?[] args)
         => string.Format(CultureInfo.CurrentCulture, Get(name), args);

      /// <summary>
      /// True when <paramref name="value"/> starts with the current-culture
      /// prefix or the English fallback, so a language switch still finds an
      /// unsaved "new service/account" placeholder.
      /// </summary>
      public static bool IsPlaceholderName(string value, string prefixKey)
      {
         if (string.IsNullOrEmpty(value))
         {
            return false;
         }

         string current = Get(prefixKey);
         if (value.StartsWith(current, StringComparison.Ordinal))
         {
            return true;
         }

         string neutral = GetNeutral(prefixKey);
         return !string.Equals(current, neutral, StringComparison.Ordinal)
            && value.StartsWith(neutral, StringComparison.Ordinal);
      }

      public static string Activity_MergeAndSaveThenRemoveAutoSaveFile => Get(nameof(Activity_MergeAndSaveThenRemoveAutoSaveFile));
      public static string Activity_MergeWithoutSavingAndKeepAutoSaveFile => Get(nameof(Activity_MergeWithoutSavingAndKeepAutoSaveFile));
      public static string Activity_DontMergeAndRemoveAutoSaveFile => Get(nameof(Activity_DontMergeAndRemoveAutoSaveFile));
      public static string Activity_DontMergeAndKeepAutoSaveFile => Get(nameof(Activity_DontMergeAndKeepAutoSaveFile));
      public static string Activity_DatabaseCreated => Get(nameof(Activity_DatabaseCreated));
      public static string Activity_DatabaseOpened => Get(nameof(Activity_DatabaseOpened));
      public static string Activity_DatabaseSaved => Get(nameof(Activity_DatabaseSaved));
      public static string Activity_DatabaseClosed => Get(nameof(Activity_DatabaseClosed));
      public static string Activity_DateTimeFormat => Get(nameof(Activity_DateTimeFormat));
      public static string Activity_LoginSessionTimeoutReached => Get(nameof(Activity_LoginSessionTimeoutReached));
      public static string Activity_LoginFailed => Get(nameof(Activity_LoginFailed));
      public static string Activity_UserLoggedIn => Get(nameof(Activity_UserLoggedIn));
      public static string Activity_UserLoggedOut => Get(nameof(Activity_UserLoggedOut));
      public static string Activity_UserLoggedOutWithoutSaving => Get(nameof(Activity_UserLoggedOutWithoutSaving));
      public static string Activity_ImportingDataStarted => Get(nameof(Activity_ImportingDataStarted));
      public static string Activity_ImportingDataSucceded => Get(nameof(Activity_ImportingDataSucceded));
      public static string Activity_ImportingDataFailed => Get(nameof(Activity_ImportingDataFailed));
      public static string Activity_ExportingDataStarted => Get(nameof(Activity_ExportingDataStarted));
      public static string Activity_ExportingDataSucceded => Get(nameof(Activity_ExportingDataSucceded));
      public static string Activity_ExportingDataFailed => Get(nameof(Activity_ExportingDataFailed));
      public static string Activity_AccountUpdated => Get(nameof(Activity_AccountUpdated));
      public static string Activity_AccountSet => Get(nameof(Activity_AccountSet));
      public static string Activity_ItemUpdated => Get(nameof(Activity_ItemUpdated));
      public static string Activity_ItemSet => Get(nameof(Activity_ItemSet));
      public static string Activity_ItemAdded => Get(nameof(Activity_ItemAdded));
      public static string Activity_ItemDeleted => Get(nameof(Activity_ItemDeleted));
      public static string Activity_ActivityLogTampered => Get(nameof(Activity_ActivityLogTampered));
      public static string Activity_User => Get(nameof(Activity_User));
      public static string Button_Cancel => Get(nameof(Button_Cancel));
      public static string Button_No => Get(nameof(Button_No));
      public static string Button_OK => Get(nameof(Button_OK));
      public static string Button_Yes => Get(nameof(Button_Yes));
      public static string Filter_All => Get(nameof(Filter_All));
      public static string Filter_Csv => Get(nameof(Filter_Csv));
      public static string Filter_CsvExport => Get(nameof(Filter_CsvExport));
      public static string Filter_Json => Get(nameof(Filter_Json));
      public static string Filter_Pku => Get(nameof(Filter_Pku));
      public static string IdentifierType_AuthenticatorApp => Get(nameof(IdentifierType_AuthenticatorApp));
      public static string IdentifierType_Email => Get(nameof(IdentifierType_Email));
      public static string IdentifierType_Passkey => Get(nameof(IdentifierType_Passkey));
      public static string IdentifierType_PhoneNumber => Get(nameof(IdentifierType_PhoneNumber));
      public static string IdentifierType_Username => Get(nameof(IdentifierType_Username));
      public static string Label_AccountColumn => Get(nameof(Label_AccountColumn));
      public static string Label_Browse => Get(nameof(Label_Browse));
      public static string Label_BuildUpdate => Get(nameof(Label_BuildUpdate));
      public static string Label_CheckIfLeaked => Get(nameof(Label_CheckIfLeaked));
      public static string Label_CleanClipboardEvery => Get(nameof(Label_CleanClipboardEvery));
      public static string Label_Copy => Get(nameof(Label_Copy));
      public static string Label_Credentials => Get(nameof(Label_Credentials));
      public static string Label_DateAndTime => Get(nameof(Label_DateAndTime));
      public static string Label_DefaultDatabaseDirectory => Get(nameof(Label_DefaultDatabaseDirectory));
      public static string Label_DeleteOfflineDatabase => Get(nameof(Label_DeleteOfflineDatabase));
      public static string Label_Entries => Get(nameof(Label_Entries));
      public static string Label_EventType => Get(nameof(Label_EventType));
      public static string Label_EventTypeColumn => Get(nameof(Label_EventTypeColumn));
      public static string Label_Filters => Get(nameof(Label_Filters));
      public static string Label_FiltersColon => Get(nameof(Label_FiltersColon));
      public static string Label_From => Get(nameof(Label_From));
      public static string Label_Go => Get(nameof(Label_Go));
      public static string Label_GoToAccount => Get(nameof(Label_GoToAccount));
      public static string Label_GoToItem => Get(nameof(Label_GoToItem));
      public static string Label_Identifier => Get(nameof(Label_Identifier));
      public static string Label_Identifiers => Get(nameof(Label_Identifiers));
      public static string Label_Label => Get(nameof(Label_Label));
      public static string Label_Language => Get(nameof(Label_Language));
      public static string Label_UseAppLanguage => Get(nameof(Label_UseAppLanguage));
      public static string Label_Theme => Get(nameof(Label_Theme));
      public static string Label_UseAppTheme => Get(nameof(Label_UseAppTheme));
      public static string Label_Length => Get(nameof(Label_Length));
      public static string Label_LimitActivityHistory => Get(nameof(Label_LimitActivityHistory));
      public static string Label_LimitPasswordHistory => Get(nameof(Label_LimitPasswordHistory));
      public static string Label_LogoutAfter => Get(nameof(Label_LogoutAfter));
      public static string Label_LowerCaseAlphabet => Get(nameof(Label_LowerCaseAlphabet));
      public static string Label_Message => Get(nameof(Label_Message));
      public static string Label_MessageColumn => Get(nameof(Label_MessageColumn));
      public static string Label_Milliseconds => Get(nameof(Label_Milliseconds));
      public static string Label_Minutes => Get(nameof(Label_Minutes));
      public static string Label_Months => Get(nameof(Label_Months));
      public static string Label_NeedsReview => Get(nameof(Label_NeedsReview));
      public static string Label_NeedsReviewOnly => Get(nameof(Label_NeedsReviewOnly));
      public static string Label_NewUser => Get(nameof(Label_NewUser));
      public static string Label_Notes => Get(nameof(Label_Notes));
      public static string Label_OfflineLeakDatabase => Get(nameof(Label_OfflineLeakDatabase));
      public static string Label_NotifyActivityReview => Get(nameof(Label_NotifyActivityReview));
      public static string Label_NotifyDuplicatedPasswords => Get(nameof(Label_NotifyDuplicatedPasswords));
      public static string Label_NotifyPasswordLeaked => Get(nameof(Label_NotifyPasswordLeaked));
      public static string Label_NotifyPasswordUpdateReminder => Get(nameof(Label_NotifyPasswordUpdateReminder));
      public static string Label_Numerics => Get(nameof(Label_Numerics));
      public static string Label_Options => Get(nameof(Label_Options));
      public static string Label_Password => Get(nameof(Label_Password));
      public static string Label_PasswordGroup => Get(nameof(Label_PasswordGroup));
      public static string Label_Passwords => Get(nameof(Label_Passwords));
      public static string Label_RemindPasswordUpdate => Get(nameof(Label_RemindPasswordUpdate));
      public static string Label_Reviewed => Get(nameof(Label_Reviewed));
      public static string Label_Seconds => Get(nameof(Label_Seconds));
      public static string Label_Service => Get(nameof(Label_Service));
      public static string Label_ServiceColumn => Get(nameof(Label_ServiceColumn));
      public static string Label_ServiceName => Get(nameof(Label_ServiceName));
      public static string Label_Settings => Get(nameof(Label_Settings));
      public static string Label_ShowQrCodeDuring => Get(nameof(Label_ShowQrCodeDuring));
      public static string Label_SpecialCharacters => Get(nameof(Label_SpecialCharacters));
      public static string Label_Text => Get(nameof(Label_Text));
      public static string Label_To => Get(nameof(Label_To));
      public static string Label_UnsavedItemsOnly => Get(nameof(Label_UnsavedItemsOnly));
      public static string Label_UpperCaseAlphabet => Get(nameof(Label_UpperCaseAlphabet));
      public static string Label_Url => Get(nameof(Label_Url));
      public static string Label_UseOfflineBloomFilter => Get(nameof(Label_UseOfflineBloomFilter));
      public static string Label_Username => Get(nameof(Label_Username));
      public static string Label_WarnDuplicatedPassword => Get(nameof(Label_WarnDuplicatedPassword));
      public static string Label_Warnings => Get(nameof(Label_Warnings));
      public static string Label_WarningType => Get(nameof(Label_WarningType));
      public static string Label_WarningTypeColumn => Get(nameof(Label_WarningTypeColumn));
      public static string Label_WarnPasswordLeak => Get(nameof(Label_WarnPasswordLeak));
      public static string Menu_AppSettings => Get(nameof(Menu_AppSettings));
      public static string Menu_Copy => Get(nameof(Menu_Copy));
      public static string Menu_DeleteUser => Get(nameof(Menu_DeleteUser));
      public static string Menu_Export => Get(nameof(Menu_Export));
      public static string Menu_ExportCsv => Get(nameof(Menu_ExportCsv));
      public static string Menu_ExportJson => Get(nameof(Menu_ExportJson));
      public static string Menu_GenerateRandomPassword => Get(nameof(Menu_GenerateRandomPassword));
      public static string Menu_Import => Get(nameof(Menu_Import));
      public static string Menu_Insert => Get(nameof(Menu_Insert));
      public static string Menu_Logout => Get(nameof(Menu_Logout));
      public static string Menu_NewUser => Get(nameof(Menu_NewUser));
      public static string Menu_OpenDatabase => Get(nameof(Menu_OpenDatabase));
      public static string Menu_Regenerate => Get(nameof(Menu_Regenerate));
      public static string Menu_ResetToDefault => Get(nameof(Menu_ResetToDefault));
      public static string Menu_Save => Get(nameof(Menu_Save));
      public static string Menu_ShowActivities => Get(nameof(Menu_ShowActivities));
      public static string Menu_UserSettings => Get(nameof(Menu_UserSettings));
      public static string Menu_ViewActivities => Get(nameof(Menu_ViewActivities));
      public static string Msg_AccountId => Get(nameof(Msg_AccountId));
      public static string Msg_AtLeastOnePassword => Get(nameof(Msg_AtLeastOnePassword));
      public static string Msg_AutosaveDetected => Get(nameof(Msg_AutosaveDetected));
      public static string Msg_BuildFailed => Get(nameof(Msg_BuildFailed));
      public static string Msg_BuildOfflineLeakDatabase => Get(nameof(Msg_BuildOfflineLeakDatabase));
      public static string Msg_CheckingPasskey => Get(nameof(Msg_CheckingPasskey));
      public static string Msg_ConfigFileError => Get(nameof(Msg_ConfigFileError));
      public static string Msg_CorruptedDatabase => Get(nameof(Msg_CorruptedDatabase));
      public static string Msg_CredentialsUpdated => Get(nameof(Msg_CredentialsUpdated));
      public static string Msg_DatabaseLabel => Get(nameof(Msg_DatabaseLabel));
      public static string Msg_DeleteAccount => Get(nameof(Msg_DeleteAccount));
      public static string Msg_DeleteService => Get(nameof(Msg_DeleteService));
      public static string Msg_DeleteOfflineLeakDatabase => Get(nameof(Msg_DeleteOfflineLeakDatabase));
      public static string Msg_DeleteUserConfirm1 => Get(nameof(Msg_DeleteUserConfirm1));
      public static string Msg_DeleteUserConfirm2 => Get(nameof(Msg_DeleteUserConfirm2));
      public static string Msg_DuplicatedPasswordAccounts => Get(nameof(Msg_DuplicatedPasswordAccounts));
      public static string Msg_ExportData => Get(nameof(Msg_ExportData));
      public static string Msg_ExportFailed => Get(nameof(Msg_ExportFailed));
      public static string Msg_ExportSuccess => Get(nameof(Msg_ExportSuccess));
      public static string Msg_FiltersHeader => Get(nameof(Msg_FiltersHeader));
      public static string Msg_ImportData => Get(nameof(Msg_ImportData));
      public static string Msg_ImportFailed => Get(nameof(Msg_ImportFailed));
      public static string Msg_ImportSuccess => Get(nameof(Msg_ImportSuccess));
      public static string Msg_InsufficientKdf => Get(nameof(Msg_InsufficientKdf));
      public static string Msg_ItemNotFound => Get(nameof(Msg_ItemNotFound));
      public static string Msg_NewAccountPrefix => Get(nameof(Msg_NewAccountPrefix));
      public static string Msg_NewServicePrefix => Get(nameof(Msg_NewServicePrefix));
      public static string Msg_NoDatabaseLoaded => Get(nameof(Msg_NoDatabaseLoaded));
      public static string Msg_NoOfflineLeakDatabase => Get(nameof(Msg_NoOfflineLeakDatabase));
      public static string Msg_NoPasswordEmpty => Get(nameof(Msg_NoPasswordEmpty));
      public static string Msg_OfflineLeakAlreadyUpToDate => Get(nameof(Msg_OfflineLeakAlreadyUpToDate));
      public static string Msg_OfflineLeakBuildComplete => Get(nameof(Msg_OfflineLeakBuildComplete));
      public static string Msg_OfflineLeakBuildFailed => Get(nameof(Msg_OfflineLeakBuildFailed));
      public static string Msg_OfflineLeakBuildProgress => Get(nameof(Msg_OfflineLeakBuildProgress));
      public static string Msg_OfflineLeakBuildSkipped => Get(nameof(Msg_OfflineLeakBuildSkipped));
      public static string Msg_OfflineLeakBuildStarting => Get(nameof(Msg_OfflineLeakBuildStarting));
      public static string Msg_OfflineLeakFileAbsent => Get(nameof(Msg_OfflineLeakFileAbsent));
      public static string Msg_OfflineLeakFilePresent => Get(nameof(Msg_OfflineLeakFilePresent));
      public static string Msg_OfflineLeakFilePresentDisabled => Get(nameof(Msg_OfflineLeakFilePresentDisabled));
      public static string Msg_OfflineLeakStatusUnknown => Get(nameof(Msg_OfflineLeakStatusUnknown));
      public static string Msg_OfflineLeakUpdateComplete => Get(nameof(Msg_OfflineLeakUpdateComplete));
      public static string Msg_OfflineLeakUpdateProgress => Get(nameof(Msg_OfflineLeakUpdateProgress));
      public static string Msg_OpeningDatabase => Get(nameof(Msg_OpeningDatabase));
      public static string Msg_UpdateOfflineLeakDatabase => Get(nameof(Msg_UpdateOfflineLeakDatabase));
      public static string Msg_SaveBeforeContinue => Get(nameof(Msg_SaveBeforeContinue));
      public static string Msg_ServiceId => Get(nameof(Msg_ServiceId));
      public static string Msg_SessionLeftTime => Get(nameof(Msg_SessionLeftTime));
      public static string Msg_ShowActivityWarnings => Get(nameof(Msg_ShowActivityWarnings));
      public static string Msg_ShowDuplicatedPasswordWarnings => Get(nameof(Msg_ShowDuplicatedPasswordWarnings));
      public static string Msg_ShowExpiredPasswordWarnings => Get(nameof(Msg_ShowExpiredPasswordWarnings));
      public static string Msg_ShowLeakedPasswordWarnings => Get(nameof(Msg_ShowLeakedPasswordWarnings));
      public static string Msg_ShowWarnings => Get(nameof(Msg_ShowWarnings));
      public static string Msg_UseDefaultLocation => Get(nameof(Msg_UseDefaultLocation));
      public static string Msg_UserCreated => Get(nameof(Msg_UserCreated));
      public static string Msg_UserDeleted => Get(nameof(Msg_UserDeleted));
      public static string Msg_UserId => Get(nameof(Msg_UserId));
      public static string Msg_UsernameEmpty => Get(nameof(Msg_UsernameEmpty));
      public static string Msg_UserUpdated => Get(nameof(Msg_UserUpdated));
      public static string Title_AccountPasswordsWarnings => Get(nameof(Title_AccountPasswordsWarnings));
      public static string Title_Activities => Get(nameof(Title_Activities));
      public static string Title_AppSettings => Get(nameof(Title_AppSettings));
      public static string Title_AutosaveDetected => Get(nameof(Title_AutosaveDetected));
      public static string Title_BrowseDatabaseDirectory => Get(nameof(Title_BrowseDatabaseDirectory));
      public static string Title_BuildFailed => Get(nameof(Title_BuildFailed));
      public static string Title_BuildOfflineLeakDatabase => Get(nameof(Title_BuildOfflineLeakDatabase));
      public static string Title_ConfigFileError => Get(nameof(Title_ConfigFileError));
      public static string Title_ConfirmationRequired => Get(nameof(Title_ConfirmationRequired));
      public static string Title_CorruptedDatabase => Get(nameof(Title_CorruptedDatabase));
      public static string Title_DeleteAccount => Get(nameof(Title_DeleteAccount));
      public static string Title_DeleteOfflineLeakDatabase => Get(nameof(Title_DeleteOfflineLeakDatabase));
      public static string Title_DeleteService => Get(nameof(Title_DeleteService));
      public static string Title_DuplicatedPasswordsWarnings => Get(nameof(Title_DuplicatedPasswordsWarnings));
      public static string Title_Error => Get(nameof(Title_Error));
      public static string Title_ExportCsv => Get(nameof(Title_ExportCsv));
      public static string Title_ExportFailed => Get(nameof(Title_ExportFailed));
      public static string Title_ExportJson => Get(nameof(Title_ExportJson));
      public static string Title_ExportSuccess => Get(nameof(Title_ExportSuccess));
      public static string Title_ImportData => Get(nameof(Title_ImportData));
      public static string Title_ImportFailed => Get(nameof(Title_ImportFailed));
      public static string Title_ImportSuccess => Get(nameof(Title_ImportSuccess));
      public static string Title_InsertIdentifier => Get(nameof(Title_InsertIdentifier));
      public static string Title_InsufficientKdf => Get(nameof(Title_InsufficientKdf));
      public static string Title_ItemNotFound => Get(nameof(Title_ItemNotFound));
      public static string Title_NewUser => Get(nameof(Title_NewUser));
      public static string Title_NewUserDatabase => Get(nameof(Title_NewUserDatabase));
      public static string Title_OfflineLeakDatabase => Get(nameof(Title_OfflineLeakDatabase));
      public static string Title_OpenDatabase => Get(nameof(Title_OpenDatabase));
      public static string Title_PasswordGenerator => Get(nameof(Title_PasswordGenerator));
      public static string Title_QrCode => Get(nameof(Title_QrCode));
      public static string Title_Success => Get(nameof(Title_Success));
      public static string Title_UpdateOfflineLeakDatabase => Get(nameof(Title_UpdateOfflineLeakDatabase));
      public static string Title_UseDefaultLocation => Get(nameof(Title_UseDefaultLocation));
      public static string Title_UserSettings => Get(nameof(Title_UserSettings));
      public static string Title_UserServices => Get(nameof(Title_UserServices));
      public static string FieldName_Label => Get(nameof(FieldName_Label));
      public static string FieldName_Identifiers => Get(nameof(FieldName_Identifiers));
      public static string FieldName_Password => Get(nameof(FieldName_Password));
      public static string FieldName_Notes => Get(nameof(FieldName_Notes));
      public static string FieldName_PasswordUpdateReminderDelay => Get(nameof(FieldName_PasswordUpdateReminderDelay));
      public static string FieldName_Options => Get(nameof(FieldName_Options));
      public static string FieldName_ServiceName => Get(nameof(FieldName_ServiceName));
      public static string FieldName_Url => Get(nameof(FieldName_Url));
      public static string FieldName_LogoutTimeout => Get(nameof(FieldName_LogoutTimeout));
      public static string FieldName_CleaningClipboardTimeout => Get(nameof(FieldName_CleaningClipboardTimeout));
      public static string FieldName_ShowPasswordDelay => Get(nameof(FieldName_ShowPasswordDelay));
      public static string FieldName_NumberOfOldPasswordToKeep => Get(nameof(FieldName_NumberOfOldPasswordToKeep));
      public static string FieldName_NumberOfMonthActivitiesToKeep => Get(nameof(FieldName_NumberOfMonthActivitiesToKeep));
      public static string FieldName_WarningsToNotify => Get(nameof(FieldName_WarningsToNotify));
      public static string FieldName_Username => Get(nameof(FieldName_Username));
      public static string FieldName_Passkeys => Get(nameof(FieldName_Passkeys));
      public static string FieldName_Language => Get(nameof(FieldName_Language));
      public static string FieldName_Theme => Get(nameof(FieldName_Theme));
      public static string EnumValue_None => Get(nameof(EnumValue_None));
      public static string EnumValue_FollowApp => Get(nameof(EnumValue_FollowApp));
      public static string EnumValue_Theme_System => Get(nameof(EnumValue_Theme_System));
      public static string EnumValue_Theme_Light => Get(nameof(EnumValue_Theme_Light));
      public static string EnumValue_Theme_Dark => Get(nameof(EnumValue_Theme_Dark));
      public static string EnumValue_ImportExportError_None => Get(nameof(EnumValue_ImportExportError_None));
      public static string EnumValue_ImportExportError_ImportFileNotAccessible => Get(nameof(EnumValue_ImportExportError_ImportFileNotAccessible));
      public static string EnumValue_ImportExportError_ExtentionFileNotSupported => Get(nameof(EnumValue_ImportExportError_ExtentionFileNotSupported));
      public static string EnumValue_ImportExportError_CSVHeadersDontMatch => Get(nameof(EnumValue_ImportExportError_CSVHeadersDontMatch));
      public static string EnumValue_ImportExportError_IncorrectCSVFormat => Get(nameof(EnumValue_ImportExportError_IncorrectCSVFormat));
      public static string EnumValue_ImportExportError_NoDataToImport => Get(nameof(EnumValue_ImportExportError_NoDataToImport));
      public static string EnumValue_ImportExportError_ImportFileDeserializationFailed => Get(nameof(EnumValue_ImportExportError_ImportFileDeserializationFailed));
      public static string EnumValue_ImportExportError_ServiceAlreadyExists => Get(nameof(EnumValue_ImportExportError_ServiceAlreadyExists));
      public static string EnumValue_ImportExportError_BlankService => Get(nameof(EnumValue_ImportExportError_BlankService));
      public static string EnumValue_ImportExportError_ExportFileAlreadyExists => Get(nameof(EnumValue_ImportExportError_ExportFileAlreadyExists));
      public static string Tooltip_OfflineLeakDatabase => Get(nameof(Tooltip_OfflineLeakDatabase));
      public static string Tooltip_AddAccount => Get(nameof(Tooltip_AddAccount));
      public static string Tooltip_AddIdentifier => Get(nameof(Tooltip_AddIdentifier));
      public static string Tooltip_AddPassword => Get(nameof(Tooltip_AddPassword));
      public static string Tooltip_AddService => Get(nameof(Tooltip_AddService));
      public static string Tooltip_Clear => Get(nameof(Tooltip_Clear));
      public static string Tooltip_ClearFilters => Get(nameof(Tooltip_ClearFilters));
      public static string Tooltip_Copy => Get(nameof(Tooltip_Copy));
      public static string Tooltip_CopyIdentifier => Get(nameof(Tooltip_CopyIdentifier));
      public static string Tooltip_CopyPassword => Get(nameof(Tooltip_CopyPassword));
      public static string Tooltip_DeleteAccount => Get(nameof(Tooltip_DeleteAccount));
      public static string Tooltip_DeleteIdentifier => Get(nameof(Tooltip_DeleteIdentifier));
      public static string Tooltip_DeletePassword => Get(nameof(Tooltip_DeletePassword));
      public static string Tooltip_DeleteService => Get(nameof(Tooltip_DeleteService));
      public static string Tooltip_GoToItem => Get(nameof(Tooltip_GoToItem));
      public static string Tooltip_MoveDown => Get(nameof(Tooltip_MoveDown));
      public static string Tooltip_MoveUp => Get(nameof(Tooltip_MoveUp));
      public static string Tooltip_OpenUrl => Get(nameof(Tooltip_OpenUrl));
      public static string Tooltip_RevealPassword => Get(nameof(Tooltip_RevealPassword));
      public static string Tooltip_ShowQrCodeIdentifier => Get(nameof(Tooltip_ShowQrCodeIdentifier));
      public static string Tooltip_ShowQrCodePassword => Get(nameof(Tooltip_ShowQrCodePassword));
      public static string Tooltip_ViewActivities => Get(nameof(Tooltip_ViewActivities));

   }
}
