namespace Upsilon.Apps.Passkey.GUI.MAUI.ViewModels
{
   internal sealed class MainPageViewModel
   {
      public static string AppTitle => Helper.AppInfo.Title;

      /// <summary>
      /// Window title, optionally including the login idle countdown suffix.
      /// </summary>
      public string WindowTitle
      {
         get;
         set;
      } = AppTitle;

      public string CredentialsLabel { get; set; } = "Username : ";

      public string ActualCredential { get; set; } = string.Empty;

      public bool DatabaseOpened { get; set; }
   }
}
