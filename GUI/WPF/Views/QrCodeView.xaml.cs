using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for QrCodeView.xaml
   /// </summary>
   public partial class QrCodeView : Window
   {
      private QrCodeView(string qrCode, int delay)
      {
         InitializeComponent();

         Title = MainViewModel.AppTitle;
         _qrCode_I.Source = _getBitmap(qrCode);

         if (delay != 0)
         {
            DispatcherTimer timer = new()
            {
               Interval = new TimeSpan(0, 0, 0, 0, delay),
               IsEnabled = true,
            };

            timer.Tick += _timer_Elapsed;
         }

         Loaded += (s, e) => DarkMode.SetDarkMode(this);
      }

      private void _timer_Elapsed(object? sender, EventArgs e)
      {
         (sender as DispatcherTimer)?.Stop();
         DialogResult = true;
      }

      public static void ShowQrCode(Window owner, string qrCode, int delay)
      {
         if (!string.IsNullOrEmpty(qrCode))
         {
            _ = new QrCodeView(qrCode, delay)
            {
               Owner = owner
            }
            .ShowDialog();
         }
      }

      public static void CopyToClipboard(string text)
      {
         Clipboard.SetText(text);
      }

      private static BitmapImage _getBitmap(string content)
      {
         QrCode qrCode = new(content);

         int unit = 20;
         int height = qrCode.QRCodeMatrix.GetLength(0);
         int width = qrCode.QRCodeMatrix.GetLength(1);

         Bitmap bitmap = new((height + 2) * unit, (width + 2) * unit);

         using (Graphics g = Graphics.FromImage(bitmap))
         {
            g.FillRectangle(Brushes.White, 0, 0, (height + 2) * unit, (width + 2) * unit);

            for (int i = 0; i < height; i++)
            {
               for (int j = 0; j < width; j++)
               {
                  if (qrCode.QRCodeMatrix[i, j])
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