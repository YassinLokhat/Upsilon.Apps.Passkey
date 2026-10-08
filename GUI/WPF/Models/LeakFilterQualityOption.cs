namespace Upsilon.Apps.Passkey.GUI.WPF.Models
{
   /// <summary>
   /// A Bloom quality preset shown in the App Settings combo box.
   /// <paramref name="Code"/> is the persisted <c>LocalLeakDatabaseQuality</c> value.
   /// </summary>
   internal sealed record LeakFilterQualityOption(string Code, string DisplayName);
}
