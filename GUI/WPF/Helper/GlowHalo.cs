using System.Windows;
using System.Windows.Media;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>How the glowing source is shaped (what a blur would spread out).</summary>
   internal enum GlowSource
   {
      /// <summary>A filled shape (a button fill, a badge, a dot): the glow spreads outwards.</summary>
      Fill,

      /// <summary>A 1 px outline on the inner edge (a focus or selection ring): the glow spreads both ways.</summary>
      Line,
   }

   /// <summary>
   /// The soft accent glow, drawn once as a few concentric 1 px strokes whose
   /// opacity follows the falloff of a Gaussian blur, instead of a live
   /// <c>DropShadowEffect</c>. An effect makes WPF render its element into an
   /// off-screen surface and blur it again every time anything over it redraws
   /// (each frame of a hover fade or a gliding highlight); this element is plain
   /// retained drawing, so moving it or fading its opacity costs nothing extra.
   /// <para>
   /// It draws outside its own bounds (place it behind the element it lights,
   /// with the same size). With <see cref="Fill"/> set it also draws the filled
   /// shape itself, so it can stand in for a small glowing shape (a status dot).
   /// </para>
   /// </summary>
   internal sealed class GlowHalo : FrameworkElement
   {
      // A WPF blur radius covers about three standard deviations.
      private const double RADIUS_PER_SIGMA = 3;

      public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
         nameof(Color), typeof(Color), typeof(GlowHalo), new FrameworkPropertyMetadata(Colors.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

      public static readonly DependencyProperty StrengthProperty = DependencyProperty.Register(
         nameof(Strength), typeof(double), typeof(GlowHalo), new FrameworkPropertyMetadata(0.28, FrameworkPropertyMetadataOptions.AffectsRender));

      public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register(
         nameof(Radius), typeof(double), typeof(GlowHalo), new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.AffectsRender));

      public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
         nameof(Source), typeof(GlowSource), typeof(GlowHalo), new FrameworkPropertyMetadata(GlowSource.Fill, FrameworkPropertyMetadataOptions.AffectsRender));

      public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
         nameof(CornerRadius), typeof(double), typeof(GlowHalo), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

      public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
         nameof(Fill), typeof(Brush), typeof(GlowHalo), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

      public GlowHalo()
      {
         IsHitTestVisible = false;
         SnapsToDevicePixels = false;
      }

      /// <summary>Glow colour (the theme's <c>Glow.SoftColor</c> / <c>Glow.WarningColor</c>).</summary>
      public Color Color
      {
         get => (Color)GetValue(ColorProperty);
         set => SetValue(ColorProperty, value);
      }

      /// <summary>Peak opacity, as the effect's Opacity was.</summary>
      public double Strength
      {
         get => (double)GetValue(StrengthProperty);
         set => SetValue(StrengthProperty, value);
      }

      /// <summary>Blur radius, as the effect's BlurRadius was.</summary>
      public double Radius
      {
         get => (double)GetValue(RadiusProperty);
         set => SetValue(RadiusProperty, value);
      }

      public GlowSource Source
      {
         get => (GlowSource)GetValue(SourceProperty);
         set => SetValue(SourceProperty, value);
      }

      public double CornerRadius
      {
         get => (double)GetValue(CornerRadiusProperty);
         set => SetValue(CornerRadiusProperty, value);
      }

      /// <summary>Optional: the glowing shape itself, drawn over its glow.</summary>
      public Brush? Fill
      {
         get => (Brush?)GetValue(FillProperty);
         set => SetValue(FillProperty, value);
      }

      protected override Size MeasureOverride(Size availableSize) => default;

      protected override void OnRender(DrawingContext drawingContext)
      {
         ArgumentNullException.ThrowIfNull(drawingContext);

         Rect bounds = new(RenderSize);
         if (bounds.Width <= 0 || bounds.Height <= 0)
         {
            return;
         }

         foreach ((double Offset, double Alpha) ring in Rings(Source, Radius, Strength))
         {
            byte alpha = (byte)Math.Round(Math.Clamp(ring.Alpha, 0, 1) * Color.A);
            if (alpha == 0)
            {
               continue;
            }

            Rect rect = bounds;
            rect.Inflate(ring.Offset, ring.Offset);
            if (rect.Width <= 0 || rect.Height <= 0)
            {
               continue;
            }

            double corner = Math.Max(0, CornerRadius + ring.Offset);
            drawingContext.DrawRoundedRectangle(null, _pen(Color.FromArgb(alpha, Color.R, Color.G, Color.B)), rect, corner, corner);
         }

         if (Fill is Brush fill)
         {
            drawingContext.DrawRoundedRectangle(fill, null, bounds, CornerRadius, CornerRadius);
         }
      }

      // A theme has a couple of glow colours and a few dozen ring opacities: the
      // frozen pens are shared by every halo instead of made on each draw.
      private static readonly Dictionary<Color, Pen> _pens = [];

      private static Pen _pen(Color color)
      {
         if (!_pens.TryGetValue(color, out Pen? pen))
         {
            pen = new Pen(new SolidColorBrush(color), 1);
            pen.Freeze();
            _pens[color] = pen;
         }

         return pen;
      }

      /// <summary>
      /// The 1 px rings of a glow: each ring's centre line offset from the element's
      /// edge (positive outwards) and its opacity, sampled from the Gaussian blur of
      /// the source shape.
      /// </summary>
      internal static IEnumerable<(double Offset, double Alpha)> Rings(GlowSource source, double radius, double strength)
      {
         double sigma = Math.Max(0.5, radius / RADIUS_PER_SIGMA);
         int reach = (int)Math.Ceiling(radius);

         if (source == GlowSource.Fill)
         {
            // Outside a filled edge the blur falls off as the Gaussian tail.
            for (int i = 0; i < reach; i++)
            {
               double centre = i + 0.5;
               yield return (centre, strength * _upperTail(centre / sigma));
            }

            yield break;
         }

         // A 1 px line covering [-1, 0] (just inside the edge): its blur at a
         // ring centre c is the share of a Gaussian around c that falls on it.
         for (int i = -reach; i < reach; i++)
         {
            double centre = i + 0.5;
            yield return (centre, strength * (_cdf((centre + 1) / sigma) - _cdf(centre / sigma)));
         }
      }

      private static double _upperTail(double z) => 1 - _cdf(z);

      // Standard normal CDF (Abramowitz-Stegun 7.1.26 erf, error < 1.5e-7).
      private static double _cdf(double z)
      {
         double x = Math.Abs(z) / Math.Sqrt(2);
         double t = 1 / (1 + (0.3275911 * x));
         double erf = 1 - ((((((1.061405429 * t) - 1.453152027) * t) + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t * Math.Exp(-x * x);
         return z >= 0 ? 0.5 * (1 + erf) : 0.5 * (1 - erf);
      }
   }
}
