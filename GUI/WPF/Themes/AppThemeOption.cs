namespace Upsilon.Apps.Passkey.GUI.WPF.Themes
{
   /// <summary>
   /// A theme choice shown in App / User settings combo boxes.
   /// <paramref name="Code"/> is the persisted value (<c>System</c>, <c>Light</c>,
   /// <c>Dark</c>, or empty for "follow application theme").
   /// </summary>
   internal sealed record AppThemeOption(string Code, string DisplayName);
}
