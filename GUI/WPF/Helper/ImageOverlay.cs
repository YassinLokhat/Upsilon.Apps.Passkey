using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// A bitmap laid exactly over the element it decorates (used for the theme
   /// crossfade). Never hit-testable.
   /// </summary>
   internal sealed class ImageOverlay : Adorner
   {
      private readonly Image _image;

      public ImageOverlay(UIElement adorned, BitmapSource bitmap)
         : base(adorned)
      {
         IsHitTestVisible = false;
         _image = new Image { Source = bitmap, Stretch = Stretch.Fill };
         AddVisualChild(_image);
      }

      protected override int VisualChildrenCount => 1;

      protected override Visual GetVisualChild(int index) => _image;

      protected override Size MeasureOverride(Size constraint)
      {
         _image.Measure(AdornedElement.RenderSize);
         return AdornedElement.RenderSize;
      }

      protected override Size ArrangeOverride(Size finalSize)
      {
         _image.Arrange(new Rect(AdornedElement.RenderSize));
         return finalSize;
      }
   }
}
