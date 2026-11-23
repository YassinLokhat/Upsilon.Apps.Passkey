using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// A copy of what an element draws, taken from the drawing instructions WPF
   /// already keeps for each visual (glyph runs, shapes, images), in the element's
   /// own coordinates. Taking one costs a walk of the element's visual tree:
   /// nothing is rasterized. Replayed, it draws exactly what the element drew.
   /// <para>
   /// <see cref="Matches"/> compares two copies by what they visibly draw (same
   /// glyphs at the same places in the same colours, same shapes), ignoring
   /// invisible fills such as a transparent hit-test background whose size alone
   /// changed.
   /// </para>
   /// </summary>
   internal sealed class VectorSnapshot
   {
      private readonly List<Primitive> _primitives;

      private VectorSnapshot(Drawing drawing, List<Primitive> primitives)
      {
         Drawing = drawing;
         _primitives = primitives;
      }

      /// <summary>The copied drawing, in the element's own coordinates.</summary>
      public Drawing Drawing { get; }

      /// <summary>Copies what <paramref name="element"/> and its descendants draw now.</summary>
      public static VectorSnapshot Of(Visual element)
      {
         ArgumentNullException.ThrowIfNull(element);

         DrawingGroup group = new();
         using (DrawingContext context = group.Open())
         {
            _draw(context, element, isRoot: true);
         }

         // The group references the live objects the elements draw with (a shape's
         // own pen, an element's own transform). Freezing those would lock them, and
         // the shape then fails the next time it measures (it updates its pen).
         // Freeze a deep copy of their current values instead.
         Drawing copy = group.CloneCurrentValue();
         if (copy.CanFreeze)
         {
            copy.Freeze();
         }

         List<Primitive> primitives = [];
         _flatten(copy, Matrix.Identity, 1, primitives);
         return new VectorSnapshot(copy, primitives);
      }

      /// <summary>True when both draw the same thing at the same places.</summary>
      public bool Matches(VectorSnapshot other)
      {
         ArgumentNullException.ThrowIfNull(other);

         if (_primitives.Count != other._primitives.Count)
         {
            return false;
         }

         for (int i = 0; i < _primitives.Count; i++)
         {
            if (!_primitives[i].Matches(other._primitives[i]))
            {
               return false;
            }
         }

         return true;
      }

      private static void _draw(DrawingContext context, Visual visual, bool isRoot)
      {
         if (visual is UIElement { Visibility: not Visibility.Visible })
         {
            return;
         }

         int pushes = 0;
         if (!isRoot)
         {
            Vector offset = VisualTreeHelper.GetOffset(visual);
            Transform? transform = VisualTreeHelper.GetTransform(visual);
            if (offset != default)
            {
               context.PushTransform(new TranslateTransform(offset.X, offset.Y));
               pushes++;
            }

            if (transform is not null && !transform.Value.IsIdentity)
            {
               context.PushTransform(transform);
               pushes++;
            }
         }

         if (VisualTreeHelper.GetClip(visual) is Geometry clip)
         {
            context.PushClip(clip);
            pushes++;
         }

         double opacity = VisualTreeHelper.GetOpacity(visual);
         if (opacity <= 0)
         {
            _pop(context, pushes);
            return;
         }

         if (opacity < 1)
         {
            context.PushOpacity(opacity);
            pushes++;
         }

         if (VisualTreeHelper.GetDrawing(visual) is DrawingGroup own)
         {
            context.DrawDrawing(own);
         }

         int count = VisualTreeHelper.GetChildrenCount(visual);
         for (int i = 0; i < count; i++)
         {
            if (VisualTreeHelper.GetChild(visual, i) is Visual child)
            {
               _draw(context, child, isRoot: false);
            }
         }

         _pop(context, pushes);
      }

      private static void _pop(DrawingContext context, int count)
      {
         for (int i = 0; i < count; i++)
         {
            context.Pop();
         }
      }

      private static void _flatten(Drawing drawing, Matrix matrix, double opacity, List<Primitive> primitives)
      {
         switch (drawing)
         {
            case DrawingGroup group:
               if (group.Opacity <= 0)
               {
                  return;
               }

               Matrix inner = matrix;
               if (group.Transform is { } transform)
               {
                  inner = transform.Value * matrix;
               }

               foreach (Drawing child in group.Children)
               {
                  _flatten(child, inner, opacity * group.Opacity, primitives);
               }

               break;

            case GlyphRunDrawing { GlyphRun: { } run } glyphs when _isVisible(glyphs.ForegroundBrush):
               primitives.Add(new Primitive(matrix, opacity, glyphs));
               break;

            case GeometryDrawing shape when _isVisible(shape.Brush) || _isVisible(shape.Pen?.Brush):
               primitives.Add(new Primitive(matrix, opacity, shape));
               break;

            case ImageDrawing image when image.ImageSource is not null:
               primitives.Add(new Primitive(matrix, opacity, image));
               break;

            case VideoDrawing video:
               primitives.Add(new Primitive(matrix, opacity, video));
               break;
         }
      }

      private static bool _isVisible(Brush? brush) => brush switch
      {
         null => false,
         SolidColorBrush solid => solid.Opacity > 0 && solid.Color.A > 0,
         _ => brush.Opacity > 0,
      };

      private sealed record Primitive(Matrix Matrix, double Opacity, Drawing Drawing)
      {
         public bool Matches(Primitive other)
            => Matrix == other.Matrix
               && Opacity.Equals(other.Opacity)
               && _same(Drawing, other.Drawing);

         private static bool _same(Drawing a, Drawing b) => (a, b) switch
         {
            (GlyphRunDrawing x, GlyphRunDrawing y) => _same(x.GlyphRun, y.GlyphRun) && _same(x.ForegroundBrush, y.ForegroundBrush),
            (GeometryDrawing x, GeometryDrawing y) => _same(x.Brush, y.Brush) && _same(x.Pen, y.Pen) && _same(x.Geometry, y.Geometry),
            (ImageDrawing x, ImageDrawing y) => ReferenceEquals(x.ImageSource, y.ImageSource) && x.Rect == y.Rect,
            _ => false,
         };

         private static bool _same(GlyphRun x, GlyphRun y)
            => x.BaselineOrigin == y.BaselineOrigin
               && x.FontRenderingEmSize.Equals(y.FontRenderingEmSize)
               && x.BidiLevel == y.BidiLevel
               && x.IsSideways == y.IsSideways
               && Equals(x.GlyphTypeface?.FontUri, y.GlyphTypeface?.FontUri)
               && x.GlyphTypeface?.Style == y.GlyphTypeface?.Style
               && x.GlyphTypeface?.Weight == y.GlyphTypeface?.Weight
               && x.GlyphIndices.SequenceEqual(y.GlyphIndices)
               && x.AdvanceWidths.SequenceEqual(y.AdvanceWidths)
               && (x.GlyphOffsets ?? []).SequenceEqual(y.GlyphOffsets ?? []);

         private static bool _same(Pen? x, Pen? y)
            => ReferenceEquals(x, y)
               || (x is not null && y is not null
                  && x.Thickness.Equals(y.Thickness)
                  && x.StartLineCap == y.StartLineCap
                  && x.EndLineCap == y.EndLineCap
                  && x.LineJoin == y.LineJoin
                  && _same(x.Brush, y.Brush)
                  && (ReferenceEquals(x.DashStyle, y.DashStyle)
                     || (x.DashStyle is not null && y.DashStyle is not null
                        && x.DashStyle.Offset.Equals(y.DashStyle.Offset)
                        && x.DashStyle.Dashes.SequenceEqual(y.DashStyle.Dashes))));

         private static bool _same(Geometry? x, Geometry? y)
         {
            if (ReferenceEquals(x, y))
            {
               return true;
            }

            if (x is null || y is null)
            {
               return false;
            }

            return (x, y) switch
            {
               (RectangleGeometry r1, RectangleGeometry r2) => r1.Rect == r2.Rect && r1.RadiusX.Equals(r2.RadiusX) && r1.RadiusY.Equals(r2.RadiusY) && _sameTransform(r1.Transform, r2.Transform),
               (EllipseGeometry e1, EllipseGeometry e2) => e1.Center == e2.Center && e1.RadiusX.Equals(e2.RadiusX) && e1.RadiusY.Equals(e2.RadiusY) && _sameTransform(e1.Transform, e2.Transform),
               (LineGeometry l1, LineGeometry l2) => l1.StartPoint == l2.StartPoint && l1.EndPoint == l2.EndPoint && _sameTransform(l1.Transform, l2.Transform),
               _ => x.GetType() == y.GetType()
                  && x.Bounds == y.Bounds
                  && string.Equals(x.ToString(CultureInfo.InvariantCulture), y.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal),
            };
         }

         private static bool _sameTransform(Transform? x, Transform? y)
            => (x?.Value ?? Matrix.Identity) == (y?.Value ?? Matrix.Identity);

         private static bool _same(Brush? x, Brush? y)
         {
            if (ReferenceEquals(x, y))
            {
               return true;
            }

            if (x is null || y is null || x.GetType() != y.GetType() || !x.Opacity.Equals(y.Opacity) || !_sameTransform(x.Transform, y.Transform))
            {
               return false;
            }

            return (x, y) switch
            {
               (SolidColorBrush s1, SolidColorBrush s2) => s1.Color == s2.Color,
               (LinearGradientBrush g1, LinearGradientBrush g2) => g1.StartPoint == g2.StartPoint && g1.EndPoint == g2.EndPoint && _sameGradient(g1, g2),
               (RadialGradientBrush g1, RadialGradientBrush g2) => g1.Center == g2.Center && g1.GradientOrigin == g2.GradientOrigin && g1.RadiusX.Equals(g2.RadiusX) && g1.RadiusY.Equals(g2.RadiusY) && _sameGradient(g1, g2),
               _ => false,
            };
         }

         private static bool _sameGradient(GradientBrush x, GradientBrush y)
            => x.MappingMode == y.MappingMode
               && x.SpreadMethod == y.SpreadMethod
               && x.ColorInterpolationMode == y.ColorInterpolationMode
               && x.GradientStops.Count == y.GradientStops.Count
               && x.GradientStops.Zip(y.GradientStops).All(static p => p.First.Color == p.Second.Color && p.First.Offset.Equals(p.Second.Offset));
      }
   }
}
