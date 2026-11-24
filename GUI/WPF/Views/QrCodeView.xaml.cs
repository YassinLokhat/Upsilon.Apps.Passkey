using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Tools;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for QrCodeView.xaml
   /// </summary>
   internal sealed partial class QrCodeView : Window, ILanguageAware
   {
      private readonly QrCodeViewModel _viewModel;

      private QrCodeView(string qrCode, int delay)
      {
         InitializeComponent();

         _viewModel = new QrCodeViewModel(qrCode, delay);
         Title = _viewModel.Title;
         _qrCode_I.Source = _getBitmap(_viewModel.Modules);

         _viewModel.Expired += (s, e) => DialogResult = true;
         Closed += (s, e) =>
         {
            _viewModel.Close();
            _qrCode_I.Source = null;
         };
         Loaded += (s, e) => this.PostLoadSetup();
      }

      public void OnLanguageChanged()
      {
         _viewModel.OnLanguageChanged();
         Title = _viewModel.Title;
      }

      public static void ShowQrCode(string qrCode, int delay)
      {
         if (!string.IsNullOrEmpty(qrCode))
         {
            _ = AppServices.Dialogs.ShowDialog(new QrCodeView(qrCode, delay));
         }
      }

      private static BitmapImage _getBitmap(bool[][] qrCode)
      {
         int unit = 20;
         int height = qrCode.Length;
         int width = qrCode[0].Length;

         using Bitmap bitmap = new((height + 2) * unit, (width + 2) * unit);

         using (Graphics g = Graphics.FromImage(bitmap))
         {
            g.FillRectangle(Brushes.White, 0, 0, (height + 2) * unit, (width + 2) * unit);

            for (int i = 0; i < height; i++)
            {
               for (int j = 0; j < width; j++)
               {
                  if (qrCode[i][j])
                  {
                     g.FillRectangle(Brushes.Black, (i + 1) * unit, (j + 1) * unit, unit, unit);
                  }
               }
            }
         }

         using MemoryStream memory = new();
         bitmap.Save(memory, ImageFormat.Png);
         memory.Position = 0;

         BitmapImage bitmapImage = new();
         bitmapImage.BeginInit();
         bitmapImage.StreamSource = memory;
         bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
         bitmapImage.EndInit();
         bitmapImage.Freeze();

         return bitmapImage;
      }
   }
}
