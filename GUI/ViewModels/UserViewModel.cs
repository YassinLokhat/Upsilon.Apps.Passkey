using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.GUI.ViewModels
{
   internal class UserViewModel
   {
      private string _appTitle;
      public string AppTitle => _appTitle;

      private IDatabase? _database;

      public UserViewModel(IDatabase? database)
      {
         _database = database;

         _appTitle = MainViewModel.AppTitle;

         if (_database is null
            || _database.User is null)
         {
            _appTitle += " - New user";
         }
         else
         {
            _appTitle += " - User settings";
         }
      }
   }
}
