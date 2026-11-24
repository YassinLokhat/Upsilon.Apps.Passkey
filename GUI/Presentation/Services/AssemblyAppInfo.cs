using System.Reflection;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Services
{
   /// <summary>Builds <see cref="IAppInfo"/> from the host assembly name and version.</summary>
   internal sealed class AssemblyAppInfo : IAppInfo
   {
      public AssemblyAppInfo(Assembly assembly)
      {
         ArgumentNullException.ThrowIfNull(assembly);
         AssemblyName name = assembly.GetName();
         Name = name.Name ?? "Upsilon.Apps.Passkey";
         Version = name.Version?.ToString(3) ?? "0.0.0";
      }

      public string Name { get; }

      public string Version { get; }

      public string Title => $"{Name} v{Version}";
   }
}
