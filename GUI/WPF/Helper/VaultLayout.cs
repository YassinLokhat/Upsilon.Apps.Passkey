using System.ComponentModel;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// The vault window's column widths, which follow the window width on every
   /// resize step. Bound directly by the three columns
   /// (<c>{Binding ServicesWidth, Source={x:Static helper:VaultLayout.Current}}</c>):
   /// a change updates only those columns. As window resources they made WPF walk
   /// the whole window on every step to find their users, which is what made
   /// resizing struggle.
   /// </summary>
   internal sealed class VaultLayout : INotifyPropertyChanged
   {
      public static VaultLayout Current { get; } = new();

      public event PropertyChangedEventHandler? PropertyChanged;

      public double ServicesWidth { get; private set; } = 204;

      public double AccountsWidth { get; private set; } = 196;

      public double AccountColumnWidth { get; private set; } = 340;

      /// <summary>Sets the three widths, in whole pixels; only a real change notifies.</summary>
      public void Set(double services, double accounts, double accountColumn)
      {
         services = Math.Round(services);
         accounts = Math.Round(accounts);
         accountColumn = Math.Round(accountColumn);

         if (services != ServicesWidth)
         {
            ServicesWidth = services;
            _changed(nameof(ServicesWidth));
         }

         if (accounts != AccountsWidth)
         {
            AccountsWidth = accounts;
            _changed(nameof(AccountsWidth));
         }

         if (accountColumn != AccountColumnWidth)
         {
            AccountColumnWidth = accountColumn;
            _changed(nameof(AccountColumnWidth));
         }
      }

      private void _changed(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
   }
}
