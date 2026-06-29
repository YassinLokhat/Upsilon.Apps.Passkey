using System.ComponentModel;
using Microsoft.Maui.Controls;
using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Views.Controls
{
    /// <summary>
    /// Interaction logic for UserPasswordItem.xaml
    /// </summary>
    public partial class UserPasswordItem : ContentView
    {
        public readonly UserPasswordItemViewModel ViewModel;

        public event EventHandler? UpClicked;
        public event EventHandler? DownClicked;
        public event EventHandler? DeleteClicked;

        public UserPasswordItem(UserPasswordItemViewModel viewModel)
        {
            InitializeComponent();

            // En MAUI, DataContext est remplacé par BindingContext
            BindingContext = ViewModel = viewModel;
            _password_VPB.Password = ViewModel.Password;

            ViewModel.PropertyChanged += _viewModel_PropertyChanged;
            _password_VPB.PasswordChanged += _password_VPB_PasswordChanged;
        }

        public new void Focus()
        {
            _password_VPB.Focus();
        }

        private void _viewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Password" && _password_VPB.Password != ViewModel.Password)
            {
                _password_VPB.Password = ViewModel.Password;
            }
        }

        private void _password_VPB_PasswordChanged(object? sender, EventArgs e)
        {
            ViewModel.Password = _password_VPB.Password;
        }

        private void _upButton_Click(object sender, EventArgs e)
        {
            UpClicked?.Invoke(this, EventArgs.Empty);
        }

        private void _downButton_Click(object sender, EventArgs e)
        {
            DownClicked?.Invoke(this, EventArgs.Empty);
        }

        private void _deleteButton_Click(object sender, EventArgs e)
        {
            DeleteClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}