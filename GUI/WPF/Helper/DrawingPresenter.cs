using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>Replays a copied drawing (<see cref="VectorSnapshot"/>) at a fixed size; never hit-testable.</summary>
   internal sealed class DrawingPresenter : FrameworkElement
   {
      private readonly Drawing _drawing;
      private readonly Size _size;

      public DrawingPresenter(Drawing drawing, Size size)
      {
         _drawing = drawing;
         _size = size;
         IsHitTestVisible = false;
         Focusable = false;
      }

      protected override Size MeasureOverride(Size availableSize) => _size;

      protected override void OnRender(DrawingContext drawingContext)
      {
         ArgumentNullException.ThrowIfNull(drawingContext);
         drawingContext.DrawDrawing(_drawing);
      }
   }

   /// <summary>
   /// The fade-out of a part about to be hidden: a copy of its last drawing, in
   /// the adorner layer over its parent, fades and rises 4 px while the page has
   /// already moved on (layout and input are never held back).
   /// </summary>
   internal static class DrawingGhost
   {
      /// <param name="element">The part about to be hidden (still drawn).</param>
      /// <param name="size">Its size on screen.</param>
      public static void FadeOut(FrameworkElement element, Size size)
      {
         ArgumentNullException.ThrowIfNull(element);

         if (VisualTreeHelper.GetParent(element) is not UIElement parent
            || AdornerLayer.GetAdornerLayer(parent) is not AdornerLayer layer
            || size.Width <= 0
            || size.Height <= 0
            || ContentTransition.RevealsSecret(element))
         {
            return;
         }

         // Where it sat in its parent (layout offset; its own transforms are at rest).
         Vector offset = VisualTreeHelper.GetOffset(element);
         Drawing drawing = VectorSnapshot.Of(element).Drawing;
         DrawingAdorner adorner = new(parent, drawing, new Point(offset.X, offset.Y), size);
         layer.Add(adorner);
         _ = Motion.Leave(adorner.Content, () => layer.Remove(adorner));
      }

      private sealed class DrawingAdorner : Adorner
      {
         private readonly Point _place;

         public DrawingAdorner(UIElement adorned, Drawing drawing, Point place, Size size)
            : base(adorned)
         {
            IsHitTestVisible = false;
            _place = place;
            Content = new DrawingPresenter(drawing, size);
            AddVisualChild(Content);
         }

         public DrawingPresenter Content { get; }

         protected override int VisualChildrenCount => 1;

         protected override Visual GetVisualChild(int index) => Content;

         protected override Size MeasureOverride(Size constraint)
         {
            Content.Measure(constraint);
            return AdornedElement.RenderSize;
         }

         protected override Size ArrangeOverride(Size finalSize)
         {
            Content.Arrange(new Rect(_place, Content.DesiredSize));
            return finalSize;
         }
      }
   }
}
