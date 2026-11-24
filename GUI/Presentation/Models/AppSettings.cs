using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.Utils.LeakFilter;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Models
{
   /// <summary>
   /// Machine-wide application preferences persisted as JSON by
   /// <see cref="Ports.IAppSettingsStore"/>. Host-dependent paths are filled by
   /// the store from <see cref="Ports.IAppPaths"/>.
   /// </summary>
   internal sealed class AppSettings
   {
      public string DefaultDatabaseDirectory { get; set; } = string.Empty;

      /// <summary>
      /// UI language preference (<c>System</c>, <c>en</c>, <c>fr</c>, …). Machine-wide;
      /// not stored in the vault. Users may override it in their vault settings.
      /// <c>System</c> follows the OS UI language when a satellite ships.
      /// </summary>
      public string Language { get; set; } = LocalizationService.SystemCode;

      /// <summary>
      /// UI theme preference (<c>System</c>, <c>Light</c>, <c>Dark</c>). Machine-wide;
      /// not stored in the vault. Users may override it in their vault settings.
      /// </summary>
      public string Theme { get; set; } = ThemePreferences.SystemCode;

      public const int DEFAULT_LOGIN_IDLE_TIMEOUT_SECONDS = 5;

      /// <summary>
      /// Seconds of inactivity on the login screen before credentials and any
      /// half-open session are cleared. <c>0</c> disables the idle reset (not
      /// recommended; raises <c>IdleLoginDisabled</c>).
      /// </summary>
      public int LoginIdleTimeoutSeconds { get; set; } = DEFAULT_LOGIN_IDLE_TIMEOUT_SECONDS;

      public const int DEFAULT_BACKGROUND_LOCK_SECONDS = 30;

      /// <summary>
      /// Seconds the app may stay in the background before an open vault is closed on
      /// resume. <c>0</c> closes it on every return. Only hosts with
      /// <see cref="Ports.IAppLifecycle"/> (mobile) use it.
      /// </summary>
      public int BackgroundLockSeconds { get; set; } = DEFAULT_BACKGROUND_LOCK_SECONDS;

      /// <summary>
      /// When <see langword="true"/> and the <c>.pkbf</c> exists, use it after HIBP/XON fail.
      /// Disabling never deletes the on-disk filter.
      /// </summary>
      public bool LocalLeakDatabaseEnabled
      {
         get => LeakFilterConfig.Enabled;
         set => LeakFilterConfig.Enabled = value;
      }

      /// <summary>
      /// Days between background refreshes of an existing <c>.pkbf</c> at startup.
      /// <c>0</c> disables auto-update. Never triggers a first full build (too heavy
      /// for automatic use).
      /// </summary>
      public int LocalLeakDatabaseAutoUpdateFrequency
      {
         get => LeakFilterConfig.AutoUpdateFrequency;
         set => LeakFilterConfig.AutoUpdateFrequency = value;
      }

      /// <summary>
      /// Bloom quality preset (<c>Balanced</c>, <c>Strict</c>, <c>Paranoid</c>).
      /// Applied on the next full build / rebuild; incremental updates keep the
      /// on-disk filter sizing.
      /// </summary>
      public string LocalLeakDatabaseQuality
      {
         get => LeakFilterQuality.CodeFromRate(LeakFilterConfig.FalsePositiveRate);
         set => LeakFilterConfig.FalsePositiveRate = LeakFilterQuality.RateFromCode(value);
      }

      internal readonly LeakFilterConfig LeakFilterConfig = new(string.Empty);
   }
}
