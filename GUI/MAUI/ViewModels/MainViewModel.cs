using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.GUI.MAUI.OSSpecific;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.MAUI.ViewModels
{
    internal class MainViewModel : INotifyPropertyChanged
    {
        public static readonly ICryptographyCenter CryptographyCenter = new CryptographyCenter();
        public static readonly ISerializationCenter SerializationCenter = new JsonSerializationCenter();
        public static readonly IPasswordFactory PasswordFactory = new PasswordFactory();
        public static readonly IClipboardManager ClipboardManager = new OSSpecificClipboardManager();

        public static IDatabase? Database = null;

        public bool IsDatabaseLoaded => Database != null;

        private string _databaseLabel = "Aucune base chargée";
        public string DatabaseLabel
        {
            get => _databaseLabel;
            set { _databaseLabel = value; OnPropertyChanged(nameof(DatabaseLabel)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
