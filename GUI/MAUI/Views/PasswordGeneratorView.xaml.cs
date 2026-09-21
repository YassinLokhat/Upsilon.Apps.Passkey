using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using System;
using System.Threading.Tasks;
using Upsilon.Apps.Passkey.GUI.MAUI.Helper;
using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
    public partial class PasswordGenerator : ContentPage
    {
        private readonly PasswordGeneratorViewModel _viewModel;
        private TaskCompletionSource<string?>? _tcs;

        public string? GeneratedPassword { get; private set; } = null;

        public PasswordGenerator()
        {
            InitializeComponent();
            BindingContext = _viewModel = new PasswordGeneratorViewModel();

            // Ajustement de la taille de fenêtre réservé au Desktop
#if WINDOWS || MACCATALYST
            this.Loaded += (s, e) =>
            {
                if (this.Window != null)
                {
                    this.Window.Width = 550;
                    this.Window.Height = 500;

                    var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
                    this.Window.X = (displayInfo.Width / displayInfo.Density - this.Window.Width) / 2;
                    this.Window.Y = (displayInfo.Height / displayInfo.Density - this.Window.Height) / 2;
                }
            };
#endif
        }

        public static async Task<string?> ShowGeneratePasswordDialogAsync(INavigation navigation)
        {
            var page = new PasswordGenerator();
            page._tcs = new TaskCompletionSource<string?>();
            await navigation.PushModalAsync(page);
            return await page._tcs.Task;
        }

        private void _length_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            NumericTextBoxHelper.TextChanged(sender, e);
        }

        private void _regenerateMenuItem_Click(object sender, EventArgs e)
        {
            _viewModel.GeneratePassword();
        }

        private async void _copyMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_viewModel.GeneratedPassword))
            {
                await Clipboard.Default.SetTextAsync(_viewModel.GeneratedPassword);
            }
        }

        // Valide la sélection et dépile la vue modale
        private async void OnSelectPasswordClicked(object sender, EventArgs e)
        {
            GeneratedPassword = _viewModel.GeneratedPassword;
            _tcs?.TrySetResult(GeneratedPassword);
            await Navigation.PopModalAsync();
        }

        // Annule et ferme la vue modale
        private async void OnCancelClicked(object sender, EventArgs e)
        {
            _tcs?.TrySetResult(null);
            await Navigation.PopModalAsync();
        }

        // Gestion de la touche "Retour" sur Android
        protected override bool OnBackButtonPressed()
        {
            _tcs?.TrySetResult(null);
            return base.OnBackButtonPressed();
        }

        // Amélioration de l'ergonomie tactile pour le label "Leaked?"
        private void OnToggleLeakedTapped(object sender, EventArgs e)
        {
            LeakedCheckBox.IsChecked = !LeakedCheckBox.IsChecked;
        }
    }
}