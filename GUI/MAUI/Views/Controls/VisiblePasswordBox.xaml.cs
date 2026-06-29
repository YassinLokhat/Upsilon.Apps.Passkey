using System;
using Microsoft.Maui.Controls;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Views.Controls
{
    public partial class VisiblePasswordBox : ContentView
    {
        // --- Bindable Properties (Équivalent des DependencyProperties WPF) ---

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

        // --- Propriétés publiques ---

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

        // Propriétés internes pour le binding de la vue
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

        // --- Événements ---
        public event EventHandler? PasswordChanged;
        public event EventHandler? Validated;
        public event EventHandler? Aborted; // Corrigé de 'Aborded'

        public VisiblePasswordBox()
        {
            InitializeComponent();

            // En MAUI, on évite de mettre le DataContext (BindingContext) sur le composant lui-même 
            // pour ne pas casser le BindingContext hérité par l'utilisateur du contrôle.
            // C'est pourquoi le XAML utilise x:Reference this.

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
            // Déclenché quand l'utilisateur appuie sur "Entrée" ou "OK" sur le clavier virtuel
            Validated?.Invoke(this, EventArgs.Empty);
        }

        private void EyeButton_Clicked(object sender, EventArgs e)
        {
            // Mode Bascule (Toggle) : Plus adapté au multiplateforme (Mobile / Desktop)
            IsPasswordHidden = !IsPasswordHidden;
            EyeIcon = IsPasswordHidden ? "👁" : "🙈"; // Change l'icône selon l'état
        }

        // Note pour le comportement "Escape" (Annuler) de WPF :
        // Le clavier mobile n'a pas de touche Escape. Si vous ciblez uniquement le Desktop (Windows/macOS),
        // vous pouvez ajouter un KeyboardAccelerator ou intercepter les touches au niveau de la Page,
        // mais l'événement Entry.Completed gère déjà nativement la validation standard.
    }
}