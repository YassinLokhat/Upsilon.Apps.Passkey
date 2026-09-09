using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   /// <summary>
   /// Stable warning kind / source identifiers. Components publish these on
   /// <see cref="IWarning.Kind"/>; the GUI filters notify preferences by them.
   /// </summary>
   public static class WarningKinds
   {
      public const string SourceCore = "Core";
      public const string SourceHost = "Host";

      public const string ActivityReview = "ActivityReview";
      public const string PasswordUpdateReminder = "PasswordUpdateReminder";
      public const string DuplicatedPasswords = "DuplicatedPasswords";
      public const string PasswordLeaked = "PasswordLeaked";
      public const string VaultSecuritySettings = "VaultSecuritySettings";
      public const string InsufficientPasskeys = "InsufficientPasskeys";
      public const string WeakPasskey = "WeakPasskey";
      public const string PasskeyLeaked = "PasskeyLeaked";
      public const string WeakAccountPassword = "WeakAccountPassword";
      public const string PasskeyReusedAsAccountPassword = "PasskeyReusedAsAccountPassword";
      public const string HostSecuritySettings = "HostSecuritySettings";

      /// <summary>Recommended minimum onion passkey layers.</summary>
      public const int RecommendedPasskeyCount = 2;

      public static readonly string[] AllCore =
      [
         ActivityReview,
         PasswordUpdateReminder,
         DuplicatedPasswords,
         PasswordLeaked,
         VaultSecuritySettings,
         InsufficientPasskeys,
         WeakPasskey,
         PasskeyLeaked,
         WeakAccountPassword,
         PasskeyReusedAsAccountPassword,
      ];

      public static readonly string[] AllHost =
      [
         HostSecuritySettings,
      ];

      /// <summary>Default notify set for a new vault (every known kind).</summary>
      public static readonly string[] DefaultNotify =
      [
         .. AllCore,
         .. AllHost,
      ];

      /// <summary>
      /// Maps a legacy warning-type flags value (vault / autosave)
      /// onto the new kind identifiers.
      /// </summary>
#pragma warning disable CS0618 // Legacy WarningType migration surface
      public static string[] FromLegacyWarningType(WarningType legacy)
      {
         List<string> kinds = [];

         if (legacy.HasFlag(WarningType.ActivityReviewWarning))
         {
            kinds.Add(ActivityReview);
         }

         if (legacy.HasFlag(WarningType.PasswordUpdateReminderWarning))
         {
            kinds.Add(PasswordUpdateReminder);
         }

         if (legacy.HasFlag(WarningType.DuplicatedPasswordsWarning))
         {
            kinds.Add(DuplicatedPasswords);
         }

         if (legacy.HasFlag(WarningType.PasswordLeakedWarning))
         {
            kinds.Add(PasswordLeaked);
         }

         if (legacy.HasFlag(WarningType.SecuritySettingsWarning))
         {
            kinds.Add(VaultSecuritySettings);
            kinds.Add(HostSecuritySettings);
         }

         return [.. kinds];
      }
#pragma warning restore CS0618
   }
}
