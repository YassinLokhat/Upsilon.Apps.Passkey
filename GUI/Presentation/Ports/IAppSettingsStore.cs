using Upsilon.Apps.Passkey.GUI.Presentation.Models;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Ports
{
   /// <summary>Loads and persists the machine-wide <see cref="AppSettings"/>.</summary>
   internal interface IAppSettingsStore
   {
      AppSettings Current { get; set; }

      string ConfigFilePath { get; }

      /// <summary>
      /// Reads the config file into <see cref="Current"/>. On a missing or invalid
      /// file, writes defaults and returns <see langword="false"/>.
      /// </summary>
      bool Load();

      void Save();

      /// <summary>Settings with defaults resolved for this host.</summary>
      AppSettings CreateDefault();
   }
}
