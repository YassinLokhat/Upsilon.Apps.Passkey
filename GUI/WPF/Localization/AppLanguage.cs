namespace Upsilon.Apps.Passkey.GUI.WPF.Localization
{
   /// <summary>
   /// A UI language the client knows how to load (satellite <c>Strings.xx.resx</c>).
   /// </summary>
   /// <param name="Code">IETF language tag used for <see cref="System.Globalization.CultureInfo"/> (e.g. <c>en</c>, <c>fr</c>).</param>
   /// <param name="NativeName">Display name in that language for the settings combo box.</param>
   internal sealed record AppLanguage(string Code, string NativeName);
}
