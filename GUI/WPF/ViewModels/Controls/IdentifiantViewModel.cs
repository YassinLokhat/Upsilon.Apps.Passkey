using System.ComponentModel;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   public class IdentifierViewModel : INotifyPropertyChanged
   {
      private readonly IAccount _account;

      public Brush IdentifierBackground => _account.HasChanged("Identifiers") ? DarkMode.ChangedBrush : DarkMode.UnchangedBrush2;

      public string Identifier
      {
         get;
         set
         {
            if (field != value)
            {
               if (!value.StartsWith("[Username]", StringComparison.CurrentCultureIgnoreCase)
                  && !value.StartsWith("👤", StringComparison.CurrentCultureIgnoreCase)
                  && !value.StartsWith("[Email]", StringComparison.CurrentCultureIgnoreCase)
                  && !value.StartsWith("[📧]", StringComparison.CurrentCultureIgnoreCase)
                  && !value.StartsWith("[Phone Number]", StringComparison.CurrentCultureIgnoreCase)
                  && !value.StartsWith("🖁", StringComparison.CurrentCultureIgnoreCase)
                  && !value.StartsWith("[Passkey]", StringComparison.CurrentCultureIgnoreCase)
                  && !value.StartsWith("🗝", StringComparison.CurrentCultureIgnoreCase)
                  && !value.StartsWith("[Authentificator App]", StringComparison.CurrentCultureIgnoreCase)
                  && !value.StartsWith("📲", StringComparison.CurrentCultureIgnoreCase))
               {
                  value = "[Username]" + value;
               }

               field = value
                  .Replace("[Username]", "👤", StringComparison.CurrentCultureIgnoreCase)
                  .Replace("[Email]", "📧", StringComparison.CurrentCultureIgnoreCase)
                  .Replace("[Phone Number]", "🖁", StringComparison.CurrentCultureIgnoreCase)
                  .Replace("[Passkey]", "🗝", StringComparison.CurrentCultureIgnoreCase)
                  .Replace("[Authentificator App]", "📲", StringComparison.CurrentCultureIgnoreCase);

               OnPropertyChanged(nameof(Identifier));
            }
         }
      } = string.Empty;

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"{propertyName}Background"));
      }

      public IdentifierViewModel(IAccount account, string identifier)
      {
         _account = account;
         Identifier = identifier;
      }

      public void Refresh()
      {
         OnPropertyChanged(nameof(IdentifierBackground));
      }
   }
}
