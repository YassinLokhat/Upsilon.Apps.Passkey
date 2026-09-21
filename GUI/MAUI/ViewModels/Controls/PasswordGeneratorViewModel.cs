using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Upsilon.Apps.Passkey.GUI.MAUI.ViewModels.Controls
{
    public partial class PasswordViewModel(string updateDate, string password) : ObservableObject
    {
        [ObservableProperty]
        private string _updateDate = updateDate;

        [ObservableProperty]
        private string _password = password;
    }
}
