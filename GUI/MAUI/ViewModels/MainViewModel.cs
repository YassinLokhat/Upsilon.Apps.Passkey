using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.GUI.MAUI.OSSpecific;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.MAUI.ViewModels
{
    internal partial class MainViewModel : ObservableObject
    {
        public static readonly ICryptographyCenter CryptographyCenter = new CryptographyCenter();
        public static readonly ISerializationCenter SerializationCenter = new JsonSerializationCenter();
        public static readonly IPasswordFactory PasswordFactory = new PasswordFactory();
        public static readonly IClipboardManager ClipboardManager = new OSSpecificClipboardManager();

        public static IDatabase? Database = null;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DatabaseLabel))] 
        private string _databaseFile = string.Empty;

        [ObservableProperty]
        private string _credentialsLabel = "Username :";

        public string DatabaseLabel => File.Exists(DatabaseFile)
            ? $"Database : {Path.GetFileName(DatabaseFile)}"
            : "No database loaded.";


    }
}
