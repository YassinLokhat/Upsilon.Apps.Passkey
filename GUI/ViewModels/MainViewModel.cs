using System.ComponentModel;
using System.Windows;
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

      public readonly static ICryptographyCenter CryptographyCenter = new CryptographyCenter();
      public readonly static ISerializationCenter SerializationCenter = new JsonSerializationCenter();
      public readonly static IPasswordFactory PasswordFactory = new PasswordFactory();

      public static IDatabase? Database = null;

      private string _label = "Username :";
      public string Label
      {
         get => _label;
         set
         {
            if (_label != value)
            {
               _label = value;
               OnPropertyChanged(nameof(Label));

               if (_label == "Username :")
               {
                  UsernameVisibility = Visibility.Visible;
                  PasswordVisibility = Visibility.Hidden;
               }
               else
               {
                  UsernameVisibility = Visibility.Hidden;
                  PasswordVisibility = Visibility.Visible;
               }
            }
         }
      }

      private Visibility _usernameVisibility = Visibility.Visible;
      public Visibility UsernameVisibility
      {
         get => _usernameVisibility;
         set
         {
            if (_usernameVisibility != value)
            {
               _usernameVisibility = value;
               OnPropertyChanged(nameof(UsernameVisibility));
            }
         }
      }

      private Visibility _passwordVisibility = Visibility.Hidden;
      public Visibility PasswordVisibility
      {
         get => _passwordVisibility;
         set
         {
            if (_passwordVisibility != value)
            {
               _passwordVisibility = value;
               OnPropertyChanged(nameof(PasswordVisibility));
            }
         }
      }

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
   }
}
