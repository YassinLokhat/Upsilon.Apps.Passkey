using QRCodeEncoderLibrary;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.Helper;
using Upsilon.Apps.Passkey.GUI.Themes;
using Upsilon.Apps.Passkey.GUI.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.Views
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

         Loaded += _mainWindow_Loaded;
      }

      private void _timer_Elapsed(object? sender, EventArgs e)
      {
         (sender as DispatcherTimer)?.Stop();
         DialogResult = true;
      }

      public static void ShowQrCode(Window owner, string qrCode, int delay)
      {
         _ = new QrCodeView(qrCode, delay)
         {
            Owner = owner
         }
         .ShowDialog();
      }

      public static void CopyToClipboard(string text)
      {
         Clipboard.SetText(text);
      }

      private void _mainWindow_Loaded(object sender, RoutedEventArgs e)
      {
         DarkMode.SetDarkMode(this);
      }

      private static BitmapSource _getBitmap(string content)
      {
         QREncoder qrGenerator = new()
         {
            ErrorCorrection = ErrorCorrection.H
         };
         bool[,] matrix = qrGenerator.Encode(content);

         QRSaveBitmapImage qrCodeImage = new(matrix)
         {
            ModuleSize = 100,
            QuietZone = 50
         };

         MemoryStream ms = new();
         qrCodeImage.SaveQRCodeToImageFile(ms, ImageFormat.Bmp);

         Bitmap bitmap = (Bitmap)System.Drawing.Image.FromStream(ms);

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