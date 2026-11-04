using System;
using Microsoft.Maui.Controls;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Views.Controls
{
    public partial class VisiblePasswordBox : ContentView
    {
        
        public static readonly BindableProperty PasswordProperty =
            BindableProperty.Create(nameof(Password), typeof(string), typeof(VisiblePasswordBox), default(string), BindingMode.TwoWay);

        public static readonly BindableProperty ReadOnlyProperty =
            BindableProperty.Create(nameof(ReadOnly), typeof(bool), typeof(VisiblePasswordBox), false);

        public static readonly BindableProperty BackgroundColorProperty =
            BindableProperty.Create(nameof(BackgroundColor), typeof(Brush), typeof(VisiblePasswordBox), Brush.Transparent);

        private static readonly BindableProperty IsPasswordHiddenProperty =
            BindableProperty.Create(nameof(IsPasswordHidden), typeof(bool), typeof(VisiblePasswordBox), true);

        private static readonly BindableProperty EyeIconProperty =
            BindableProperty.Create(nameof(EyeIcon), typeof(string), typeof(VisiblePasswordBox), "👁");

        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        public bool ReadOnly
        {
            get => (bool)GetValue(ReadOnlyProperty);
            set => SetValue(ReadOnlyProperty, value);
        }

        public Brush BackgroundColor
        {
            get => (Brush)GetValue(BackgroundColorProperty);
            set => SetValue(BackgroundColorProperty, value);
        }

        internal bool IsPasswordHidden
        {
            get => (bool)GetValue(IsPasswordHiddenProperty);
            set => SetValue(IsPasswordHiddenProperty, value);
        }

        internal string EyeIcon
        {
            get => (string)GetValue(EyeIconProperty);
            set => SetValue(EyeIconProperty, value);
        }

        public event EventHandler? PasswordChanged;
        public event EventHandler? Validated;
        public event EventHandler? Aborted; 

        public VisiblePasswordBox()
        {
            InitializeComponent();

            _passwordEntry.Unfocused += _passwordEntry_Unfocused;
        }

        public new bool Focus()
        {
            return _passwordEntry.Focus();
        }

        private void _passwordEntry_Unfocused(object? sender, FocusEventArgs e)
        {
            Validated?.Invoke(this, EventArgs.Empty);
        }

        private void _passwordEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            PasswordChanged?.Invoke(this, EventArgs.Empty);
        }

        private void _passwordEntry_Completed(object sender, EventArgs e)
        {
            Validated?.Invoke(this, EventArgs.Empty);
        }

        private void EyeButton_Clicked(object sender, EventArgs e)
        {
            IsPasswordHidden = !IsPasswordHidden;
            EyeIcon = IsPasswordHidden ? "👁" : "🙈"; 
        }

    }
}