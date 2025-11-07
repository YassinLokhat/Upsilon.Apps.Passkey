using System.ComponentModel;
using Upsilon.Apps.Passkey.GUI.Helper;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;
using Upsilon.Apps.PassKey.Core.Public.Utils;

namespace Upsilon.Apps.Passkey.GUI.ViewModels
{
   internal class MainViewModel : INotifyPropertyChanged
   {
      public static string AppTitle
      {
         get
         {
            System.Reflection.AssemblyName package = System.Reflection.Assembly.GetExecutingAssembly().GetName();
            string? packageVersion = package.Version?.ToString(2);

            return $"{package.Name} v{packageVersion}";
         }
      }

      public static readonly ICryptographyCenter CryptographyCenter = new CryptographyCenter();
      public static readonly ISerializationCenter SerializationCenter = new JsonSerializationCenter();
      public static readonly IPasswordFactory PasswordFactory = new PasswordFactory();

      public static IDatabase? Database = null;

      public static IUser User => Database is null || Database.User is null ? throw new NullReferenceException(nameof(User)) : Database.User;

      private string _label = "Username :";
      public string Label
      {
         get => _label;
         set => PropertyHelper.SetProperty(ref _label, value, this, PropertyChanged);
      }

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
   }
}
