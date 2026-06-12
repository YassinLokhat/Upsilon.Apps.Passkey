using System.ComponentModel;
using System.IO;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal class MainViewModel : INotifyPropertyChanged
   {
      public static string AppTitle => AppInfo.Title;

      public string DatabaseFile
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged, nameof(DatabaseLabel));
      } = string.Empty;

      public string DatabaseLabel => File.Exists(DatabaseFile) ? $"Database : {Path.GetFileName(DatabaseFile)}" : "No database loaded.";

      public string CredentialsLabel
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = "Username :";

      public event PropertyChangedEventHandler? PropertyChanged;
   }
}
