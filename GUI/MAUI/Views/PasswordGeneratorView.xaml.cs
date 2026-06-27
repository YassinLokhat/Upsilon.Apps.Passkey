using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;
using Upsilon.Apps.Passkey.GUI.MAUI.Helper;

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

            // Windows/Desktop-specific resizing
#if WINDOWS
            this.Loaded += (s, e) =>
            {
                var window = this.Window;
                window.Width = 550;
                window.Height = 450;

                var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
                window.X = (displayInfo.Width / displayInfo.Density - window.Width) / 2;
                window.Y = (displayInfo.Height / displayInfo.Density - window.Height) / 2;
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

        private void _copyMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.Default.SetTextAsync(_viewModel.GeneratedPassword);
        }

        protected override bool OnBackButtonPressed()
        {
            _tcs?.TrySetResult(null);
            return base.OnBackButtonPressed();
        }
    }
}