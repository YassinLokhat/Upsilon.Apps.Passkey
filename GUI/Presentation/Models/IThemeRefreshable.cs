namespace Upsilon.Apps.Passkey.GUI.Presentation.Models
{
   /// <summary>
   /// Shared ViewModel whose <see cref="FieldVisualState"/> or <see cref="AlertSeverity"/>
   /// bindings must be re-raised after a host theme change.
   /// </summary>
   internal interface IThemeRefreshable
   {
      void OnThemeChanged();
   }
}
