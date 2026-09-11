using Upsilon.Apps.Passkey.GUI.WPF.Views.Controls;

namespace Upsilon.Apps.Passkey.GUI.WPF
{
   /// <summary>
   /// Object-creation sites for types that WPF instantiates only via BAML.
   /// Present so CA1812 treats them as used; the factories are never invoked at runtime.
   /// </summary>
   internal static class XamlBamlCreatedTypeAnchors
   {
      internal static readonly Func<object>[] Factories =
      [
         static () => new MainWindow(),
         static () => new AccountView(),
         static () => new ServiceView(),
         static () => new VisiblePasswordBox(),
         static () => new UserPasswordsContainer(),
      ];
   }
}
