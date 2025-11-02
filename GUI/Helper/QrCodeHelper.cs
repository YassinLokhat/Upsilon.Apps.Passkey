using QRCodeEncoderLibrary;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Upsilon.Apps.Passkey.GUI.Helper
{
   public static class QrCodeHelper
   {
      public static Bitmap? GetQrCode(string content)
      {
         if (string.IsNullOrEmpty(content))
         {
            return null;
         }

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

         return (Bitmap)Image.FromStream(ms);
      }
   }
}
