using System.IO;
using System.Text.Json;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.Utils.LeakFilter;

namespace Upsilon.Apps.Passkey.GUI.WPF.Models
{
   internal class AppSettings
   {
      public string DefaultDatabaseDirectory { get; set; } = Path.GetFullPath(Path.Join(Path.GetDirectoryName(Environment.ProcessPath), "raw"));

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
      public string Theme { get; set; } = LocalizationService.SystemCode;

      /// <summary>
      /// Seconds of inactivity on the login window before credentials and any
      /// half-open session are cleared. <c>0</c> disables the idle reset.
      /// </summary>
      public int LoginIdleTimeoutSeconds { get; set; } = 5;

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
      /// When <see langword="true"/>, refresh an existing <c>.pkbf</c> in the background
      /// at startup. Never triggers a first full build (too heavy for automatic use).
      /// </summary>
      public bool LocalLeakDatabaseAutoUpdateEnabled
      {
         get => LeakFilterConfig.AutoUpdateEnabled;
         set => LeakFilterConfig.AutoUpdateEnabled = value;
      }

      internal readonly LeakFilterConfig LeakFilterConfig = new(Path.GetFullPath(Path.Join(Path.GetDirectoryName(Environment.ProcessPath), "pwned-sha1.pkbf")));

      private static readonly JsonSerializerOptions _options = new() { WriteIndented = true, };
      public void Save(string configFile)
      {
         string configDirectory = Path.GetDirectoryName(configFile) ?? "./";

         if (!Directory.Exists(configDirectory))
         {
            _ = Directory.CreateDirectory(configDirectory);
         }

         File.WriteAllText(configFile, JsonSerializer.Serialize(this, _options));
      }
   }
}
