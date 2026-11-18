using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Shared motion for the Strata UI: one place for durations and easing, and
   /// small attached behaviours so views opt in with a property instead of
   /// copying storyboards. Durations live in Themes/Motion.xaml for templates;
   /// <see cref="Initialize"/> zeroes them when Windows animations are off.
   /// Animations are cosmetic only: no timer that guards a secret depends on them.
   /// </summary>
   internal static class Motion
   {
      /// <summary>False when the user turned off animations in Windows.</summary>
      public static bool Enabled => SystemParameters.ClientAreaAnimation;

      public static TimeSpan Fast => Enabled ? TimeSpan.FromMilliseconds(120) : TimeSpan.Zero;

      public static TimeSpan Popup => Enabled ? TimeSpan.FromMilliseconds(140) : TimeSpan.Zero;

      public static TimeSpan Normal => Enabled ? TimeSpan.FromMilliseconds(160) : TimeSpan.Zero;

      public static TimeSpan Slow => Enabled ? TimeSpan.FromMilliseconds(180) : TimeSpan.Zero;

      private static readonly CubicEase _easeOut = _frozenEase();

      /// <summary>
      /// Call once at start-up (before any window loads): with animations off, the
      /// template durations in Motion.xaml become zero.
      /// </summary>
      public static void Initialize(Application application)
      {
         ArgumentNullException.ThrowIfNull(application);

         if (Enabled)
         {
            return;
         }

         Duration none = new(TimeSpan.Zero);
         application.Resources["Motion.Fast"] = none;
         application.Resources["Motion.Normal"] = none;
         application.Resources["Motion.Slow"] = none;
      }

      #region Attached behaviours

      /// <summary>Window opens with a fade and a 0.98 → 1 scale.</summary>
      public static readonly DependencyProperty AnimateOpenProperty = DependencyProperty.RegisterAttached(
         "AnimateOpen", typeof(bool), typeof(Motion), new PropertyMetadata(false, _onAnimateOpenChanged));

      public static bool GetAnimateOpen(DependencyObject element) => (bool)_notNull(element).GetValue(AnimateOpenProperty);

      public static void SetAnimateOpen(DependencyObject element, bool value) => _notNull(element).SetValue(AnimateOpenProperty, value);

      /// <summary>
      /// Marks a part that moves in a layer transition (<see cref="ContentTransition"/>):
      /// its text or items crossfade and slide; the value is the cascade tier
      /// (0 service, 1 account, 2 password core), staggered 30 ms apart.
      /// </summary>
      public static readonly DependencyProperty TransitionTierProperty = DependencyProperty.RegisterAttached(
         "TransitionTier", typeof(int), typeof(Motion), new PropertyMetadata(-1));

      public static int GetTransitionTier(DependencyObject element) => (int)_notNull(element).GetValue(TransitionTierProperty);

      public static void SetTransitionTier(DependencyObject element, int value) => _notNull(element).SetValue(TransitionTierProperty, value);

      /// <summary>
      /// Marks a button whose fill glows (accent) while hovered or keyboard-focused.
      /// The glow is applied to the fill element in the template, not the label.
      /// </summary>
      public static readonly DependencyProperty GlowOnHoverProperty = DependencyProperty.RegisterAttached(
         "GlowOnHover", typeof(bool), typeof(Motion), new PropertyMetadata(false));

      public static bool GetGlowOnHover(DependencyObject element) => (bool)_notNull(element).GetValue(GlowOnHoverProperty);

      public static void SetGlowOnHover(DependencyObject element, bool value) => _notNull(element).SetValue(GlowOnHoverProperty, value);

      /// <summary>Fades in each time the element becomes visible (reveal crossfade).</summary>
      public static readonly DependencyProperty FadeInWhenShownProperty = DependencyProperty.RegisterAttached(
         "FadeInWhenShown", typeof(bool), typeof(Motion), new PropertyMetadata(false, _onFadeInWhenShownChanged));

      public static bool GetFadeInWhenShown(DependencyObject element) => (bool)_notNull(element).GetValue(FadeInWhenShownProperty);

      public static void SetFadeInWhenShown(DependencyObject element, bool value) => _notNull(element).SetValue(FadeInWhenShownProperty, value);

      /// <summary>Fades in and drops 6 px each time the element becomes visible (menus, popups).</summary>
      public static readonly DependencyProperty DropInWhenShownProperty = DependencyProperty.RegisterAttached(
         "DropInWhenShown", typeof(bool), typeof(Motion), new PropertyMetadata(false, _onDropInWhenShownChanged));

      public static bool GetDropInWhenShown(DependencyObject element) => (bool)_notNull(element).GetValue(DropInWhenShownProperty);

      public static void SetDropInWhenShown(DependencyObject element, bool value) => _notNull(element).SetValue(DropInWhenShownProperty, value);

      /// <summary>Pops (1.15 → 1) whenever the bound value changes, e.g. the "N changes" badge.</summary>
      public static readonly DependencyProperty PopOnValueProperty = DependencyProperty.RegisterAttached(
         "PopOnValue", typeof(object), typeof(Motion), new PropertyMetadata(null, _onPopOnValueChanged));

      public static object? GetPopOnValue(DependencyObject element) => _notNull(element).GetValue(PopOnValueProperty);

      public static void SetPopOnValue(DependencyObject element, object? value) => _notNull(element).SetValue(PopOnValueProperty, value);

      /// <summary>Fades from 0.35 back to 1 whenever the bound value changes (alert severity bars).</summary>
      public static readonly DependencyProperty FadeOnValueProperty = DependencyProperty.RegisterAttached(
         "FadeOnValue", typeof(object), typeof(Motion), new PropertyMetadata(null, _onFadeOnValueChanged));

      public static object? GetFadeOnValue(DependencyObject element) => _notNull(element).GetValue(FadeOnValueProperty);

      public static void SetFadeOnValue(DependencyObject element, object? value) => _notNull(element).SetValue(FadeOnValueProperty, value);

      /// <summary>
      /// The list's selection highlight glides between rows (<see cref="SelectionGlide"/>).
      /// Inherited, so the rows know to hide their own selected fill.
      /// </summary>
      public static readonly DependencyProperty GlideSelectionProperty = DependencyProperty.RegisterAttached(
         "GlideSelection", typeof(bool), typeof(Motion), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, _onGlideSelectionChanged));

      public static bool GetGlideSelection(DependencyObject element) => (bool)_notNull(element).GetValue(GlideSelectionProperty);

      public static void SetGlideSelection(DependencyObject element, bool value) => _notNull(element).SetValue(GlideSelectionProperty, value);

      private static readonly DependencyProperty _glideControllerProperty = DependencyProperty.RegisterAttached(
         "GlideController", typeof(SelectionGlide), typeof(Motion), new PropertyMetadata(null));

      #endregion

      #region Animations (each returns its storyboard, already begun and controllable)

      /// <summary>Window / dialog open: fade + scale 0.98 → 1.</summary>
      public static Storyboard Open(FrameworkElement element)
      {
         (ScaleTransform scale, _) = _transforms(element, new Point(0.5, 0.5));
         Storyboard storyboard = new();
         _add(storyboard, element, UIElement.OpacityProperty, 0, 1, Normal);
         _add(storyboard, scale, ScaleTransform.ScaleXProperty, 0.98, 1, Normal);
         _add(storyboard, scale, ScaleTransform.ScaleYProperty, 0.98, 1, Normal);
         return _begin(storyboard, element);
      }

      /// <summary>Strip / panel appearing: fade in and slide 6 px up.</summary>
      public static Storyboard Enter(FrameworkElement element)
      {
         ArgumentNullException.ThrowIfNull(element);
         element.ClearValue(_pendingCollapseProperty);

         (_, TranslateTransform translate) = _transforms(element, new Point(0.5, 0.5));
         Storyboard storyboard = new();
         _add(storyboard, element, UIElement.OpacityProperty, 0, 1, Slow);
         _add(storyboard, translate, TranslateTransform.YProperty, 6, 0, Slow);
         return _begin(storyboard, element);
      }

      /// <summary>Menus and popups: fade in and drop 6 px.</summary>
      public static Storyboard DropIn(FrameworkElement element)
      {
         (_, TranslateTransform translate) = _transforms(element, new Point(0.5, 0));
         Storyboard storyboard = new();
         _add(storyboard, element, UIElement.OpacityProperty, 0, 1, Popup);
         _add(storyboard, translate, TranslateTransform.YProperty, -6, 0, Popup);
         return _begin(storyboard, element);
      }

      /// <summary>Plain fade in.</summary>
      public static Storyboard FadeIn(FrameworkElement element)
      {
         Storyboard storyboard = new();
         _add(storyboard, element, UIElement.OpacityProperty, 0, 1, Fast);
         return _begin(storyboard, element);
      }

      /// <summary>
      /// Fade out, then collapse. If something shows the element again before the
      /// fade ends (an <see cref="Enter"/>), it stays visible.
      /// </summary>
      public static Storyboard FadeOutThenCollapse(FrameworkElement element)
      {
         ArgumentNullException.ThrowIfNull(element);

         Storyboard storyboard = new();
         _add(storyboard, element, UIElement.OpacityProperty, element.Opacity, 0, Fast);
         element.SetValue(_pendingCollapseProperty, storyboard);
         storyboard.Completed += (_, _) =>
         {
            if (element.GetValue(_pendingCollapseProperty) == storyboard)
            {
               element.ClearValue(_pendingCollapseProperty);
               element.Visibility = Visibility.Collapsed;
            }
         };
         return _begin(storyboard, element);
      }

      // The fade-out a later show cancels (see FadeOutThenCollapse).
      private static readonly DependencyProperty _pendingCollapseProperty = DependencyProperty.RegisterAttached(
         "PendingCollapse", typeof(Storyboard), typeof(Motion), new PropertyMetadata(null));

      /// <summary>Badge pop: scale 1.15 → 1.</summary>
      public static Storyboard Pop(FrameworkElement element)
      {
         (ScaleTransform scale, _) = _transforms(element, new Point(0.5, 0.5));
         Storyboard storyboard = new();
         _add(storyboard, scale, ScaleTransform.ScaleXProperty, 1.15, 1, Normal);
         _add(storyboard, scale, ScaleTransform.ScaleYProperty, 1.15, 1, Normal);
         return _begin(storyboard, element);
      }

      /// <summary>One soft pulse up and back down (the core's glow on Copy).</summary>
      public static Storyboard Pulse(FrameworkElement element)
      {
         Storyboard storyboard = new();
         TimeSpan half = Enabled ? TimeSpan.FromMilliseconds(180) : TimeSpan.Zero;
         DoubleAnimation up = new(0, 1, half) { EasingFunction = _easeOut };
         DoubleAnimation down = new(1, 0, Enabled ? TimeSpan.FromMilliseconds(360) : TimeSpan.Zero)
         {
            BeginTime = half,
            EasingFunction = _easeOut,
         };
         Storyboard.SetTarget(up, element);
         Storyboard.SetTargetProperty(up, new PropertyPath(UIElement.OpacityProperty));
         Storyboard.SetTarget(down, element);
         Storyboard.SetTargetProperty(down, new PropertyPath(UIElement.OpacityProperty));
         storyboard.Children.Add(up);
         storyboard.Children.Add(down);
         return _begin(storyboard, element);
      }

      /// <summary>
      /// A progress line shrinking from full width to nothing over
      /// <paramref name="remaining"/> (the clipboard auto-clear). Linear on
      /// purpose: it shows time, it is not a transition. Skipped when animations
      /// are off (the line then simply stays).
      /// </summary>
      public static Storyboard Progress(FrameworkElement element, TimeSpan remaining)
      {
         (ScaleTransform scale, _) = _transforms(element, new Point(0, 0.5));
         Storyboard storyboard = new();
         DoubleAnimation shrink = new(1, 0, Enabled ? remaining : TimeSpan.Zero);
         if (!Enabled)
         {
            shrink.From = 1;
            shrink.To = 1;
         }

         Storyboard.SetTarget(shrink, scale);
         Storyboard.SetTargetProperty(shrink, new PropertyPath(ScaleTransform.ScaleXProperty));
         storyboard.Children.Add(shrink);
         return _begin(storyboard, element);
      }

      #endregion

      /// <summary>
      /// Runs <paramref name="change"/> (a theme swap) behind a short crossfade:
      /// every open Strata window is snapshotted, the change repaints it underneath,
      /// and the snapshot fades out (160 ms) instead of the window flashing over.
      /// With animations off the change is simply applied.
      /// </summary>
      public static void CrossfadeTheme(Action change)
      {
         ArgumentNullException.ThrowIfNull(change);

         List<(AdornerLayer Layer, ImageOverlay Overlay)> overlays = [];
         if (Enabled && Application.Current is { } app)
         {
            foreach (Window window in app.Windows)
            {
               if (window.IsVisible
                  && VisualTreeHelper.GetChildrenCount(window) > 0
                  && VisualTreeHelper.GetChild(window, 0) is AdornerDecorator { Child: FrameworkElement root } host
                  && root.ActualWidth >= 1
                  && host.AdornerLayer is AdornerLayer layer)
               {
                  ImageOverlay overlay = new(root, ContentTransition.Render(root));
                  layer.Add(overlay);
                  overlays.Add((layer, overlay));
               }
            }
         }

         change();

         foreach ((AdornerLayer layer, ImageOverlay overlay) in overlays)
         {
            DoubleAnimation fade = new(1, 0, Normal) { EasingFunction = _easeOut };
            fade.Completed += (_, _) => layer.Remove(overlay);
            overlay.BeginAnimation(UIElement.OpacityProperty, fade);
         }
      }

      #region Behaviour handlers

      private static void _onAnimateOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
      {
         if (d is not Window window || e.NewValue is not true)
         {
            return;
         }

         window.Loaded += static (sender, args) =>
         {
            if (sender is Window loaded
               && VisualTreeHelper.GetChildrenCount(loaded) > 0
               && VisualTreeHelper.GetChild(loaded, 0) is FrameworkElement root)
            {
               _ = Open(root);
            }
         };
      }

      private static void _onFadeInWhenShownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
      {
         if (d is not FrameworkElement element || e.NewValue is not true)
         {
            return;
         }

         element.IsVisibleChanged += static (sender, args) =>
         {
            // Only a reveal toggle on a loaded control, not a row being created.
            if (sender is FrameworkElement { IsLoaded: true } shown && args.NewValue is true)
            {
               _ = FadeIn(shown);
            }
         };
      }

      private static void _onDropInWhenShownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
      {
         if (d is not FrameworkElement element || e.NewValue is not true)
         {
            return;
         }

         element.IsVisibleChanged += static (sender, args) =>
         {
            if (sender is FrameworkElement shown && args.NewValue is true)
            {
               _ = DropIn(shown);
            }
         };
      }

      private static void _onPopOnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
      {
         if (d is FrameworkElement { IsLoaded: true } element && !Equals(e.OldValue, e.NewValue))
         {
            _ = Pop(element);
         }
      }

      private static void _onFadeOnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
      {
         if (d is not FrameworkElement { IsLoaded: true } element || Equals(e.OldValue, e.NewValue))
         {
            return;
         }

         Storyboard storyboard = new();
         _add(storyboard, element, UIElement.OpacityProperty, 0.35, 1, Normal);
         _ = _begin(storyboard, element);
      }

      private static void _onGlideSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
      {
         // Only the list that opts in (not every row inheriting the value).
         if (d is not ListBox list
            || e.NewValue is not true
            || DependencyPropertyHelper.GetValueSource(list, GlideSelectionProperty).BaseValueSource == BaseValueSource.Inherited
            || list.GetValue(_glideControllerProperty) is not null)
         {
            return;
         }

         list.SetValue(_glideControllerProperty, SelectionGlide.Attach(list));
      }

      #endregion

      #region Plumbing

      /// <summary>
      /// Gives the element its own (unfrozen) scale + translate pair the first time,
      /// so animations never touch a shared or frozen transform.
      /// </summary>
      private static (ScaleTransform Scale, TranslateTransform Translate) _transforms(FrameworkElement element, Point origin)
      {
         ArgumentNullException.ThrowIfNull(element);

         if (element.RenderTransform is TransformGroup { Children: [ScaleTransform scale, TranslateTransform translate] } group
            && !group.IsFrozen)
         {
            return (scale, translate);
         }

         ScaleTransform newScale = new(1, 1);
         TranslateTransform newTranslate = new(0, 0);
         element.RenderTransform = new TransformGroup { Children = { newScale, newTranslate } };
         element.RenderTransformOrigin = origin;
         return (newScale, newTranslate);
      }

      private static void _add(Storyboard storyboard, DependencyObject target, DependencyProperty property, double from, double to, TimeSpan duration)
      {
         DoubleAnimation animation = new(from, to, duration) { EasingFunction = _easeOut };
         Storyboard.SetTarget(animation, target);
         Storyboard.SetTargetProperty(animation, new PropertyPath(property));
         storyboard.Children.Add(animation);
      }

      private static Storyboard _begin(Storyboard storyboard, FrameworkElement element)
      {
         storyboard.Begin(element, HandoffBehavior.SnapshotAndReplace, isControllable: true);
         return storyboard;
      }

      private static DependencyObject _notNull(DependencyObject element)
      {
         ArgumentNullException.ThrowIfNull(element);
         return element;
      }

      private static CubicEase _frozenEase()
      {
         CubicEase ease = new() { EasingMode = EasingMode.EaseOut };
         ease.Freeze();
         return ease;
      }

      #endregion
   }
}
