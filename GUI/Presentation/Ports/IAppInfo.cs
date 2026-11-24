namespace Upsilon.Apps.Passkey.GUI.Presentation.Ports
{
   /// <summary>Running application metadata (window / page titles).</summary>
   internal interface IAppInfo
   {
      string Name { get; }

      string Version { get; }

      /// <summary>Display title, e.g. <c>Upsilon.Apps.Passkey.GUI.WPF v2.0.0</c>.</summary>
      string Title { get; }
   }
}
