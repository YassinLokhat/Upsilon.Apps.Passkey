using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Views
{
    public partial class QrCodeView : ContentPage
    {
        private readonly IDispatcherTimer? _timer;
        private readonly bool[,] _qrMatrix;

        private QrCodeView(string qrCodeContent, int delay)
        {
            InitializeComponent();

            Title = MainViewModel.AppTitle;

            _qrMatrix = QrCode.Generate(qrCodeContent);

            _qrCodeGraphicsView.Drawable = new QrCodeDrawable(_qrMatrix);

            if (delay > 0)
            {
                _timer = Dispatcher.CreateTimer();
                _timer.Interval = TimeSpan.FromMilliseconds(delay);
                _timer.Tick += _timer_Elapsed;
                _timer.Start();
            }

            Unloaded += (s, e) => _timer?.Stop();
        }

        private void _timer_Elapsed(object? sender, EventArgs e)
        {
            _timer?.Stop();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (Navigation.ModalStack.Count > 0)
                {
                    await Navigation.PopModalAsync();
                }
            });
        }

        public static async Task ShowQrCodeAsync(string qrCode, int delay)
        {
            if (!string.IsNullOrEmpty(qrCode))
            {
                var qrPage = new QrCodeView(qrCode, delay);
                await Application.Current?.MainPage?.Navigation.PushModalAsync(qrPage)!;
            }
        }

        public static async Task CopyToClipboardAsync(string text)
        {
            await Clipboard.SetTextAsync(text);
        }
    }

    /// <label>Internal Class for Proper QR Code Rendering Across All Platforms</label>
    internal class QrCodeDrawable : IDrawable
    {
        private readonly bool[,] _matrix;

        public QrCodeDrawable(bool[,] matrix)
        {
            _matrix = matrix;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            int rows = _matrix.GetLength(0);
            int cols = _matrix.GetLength(1);

            // Fond blanc global
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(dirtyRect);

            if (rows == 0 || cols == 0) return;

            // Calcul de la taille de chaque module (pixel du QR) pour s'adapter dynamiquement à l'espace
            float moduleSize = Math.Min(dirtyRect.Width / (cols + 2), dirtyRect.Height / (rows + 2));

            // Centrage du QR code dans la zone
            float offsetX = (dirtyRect.Width - (cols * moduleSize)) / 2;
            float offsetY = (dirtyRect.Height - (rows * moduleSize)) / 2;

            canvas.FillColor = Colors.Black;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (_matrix[i, j])
                    {
                        canvas.FillRectangle(offsetX + (j * moduleSize), offsetY + (i * moduleSize), moduleSize, moduleSize);
                    }
                }
            }
        }
    }
}