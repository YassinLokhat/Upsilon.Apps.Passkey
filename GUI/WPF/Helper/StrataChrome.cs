using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Support for the custom window chrome in Themes/Controls.xaml (styles
   /// <c>StrataWindow</c> and <c>StrataDialogWindow</c>). Those styles replace the
   /// Windows title bar with WPF's built-in <see cref="System.Windows.Shell.WindowChrome"/>
   /// and draw their own caption buttons, which use the standard
   /// <see cref="SystemCommands"/>. Setting <c>UseSystemCommands</c> on a window wires
   /// those commands; the OS still handles drag, double-click, Aero Snap, edge resize
   /// and Alt+F4.
   /// </summary>
   internal static class StrataChrome
   {
      public static readonly DependencyProperty UseSystemCommandsProperty = DependencyProperty.RegisterAttached(
         "UseSystemCommands",
         typeof(bool),
         typeof(StrataChrome),
         new PropertyMetadata(false, _onUseSystemCommandsChanged));

      /// <summary>Application version shown next to the name in the title bar, e.g. "v2.0.0".</summary>
      public static string VersionLabel { get; } = $"v{_assemblyName.Version?.ToString(3) ?? "0.0.0"}";

      /// <summary>
      /// Prefix of every main window title (<see cref="AppInfo.Title"/> without forcing
      /// its config-file load): the title bar draws it as "UPSILON vX.Y.Z" instead.
      /// </summary>
      internal static string TitlePrefix { get; } = $"{_assemblyName.Name} {VersionLabel}";

      private static AssemblyName _assemblyName => Assembly.GetExecutingAssembly().GetName();

      public static bool GetUseSystemCommands(DependencyObject element)
      {
         ArgumentNullException.ThrowIfNull(element);
         return (bool)element.GetValue(UseSystemCommandsProperty);
      }

      public static void SetUseSystemCommands(DependencyObject element, bool value)
      {
         ArgumentNullException.ThrowIfNull(element);
         element.SetValue(UseSystemCommandsProperty, value);
      }

      private static void _onUseSystemCommandsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
      {
         if (d is not Window window || e.NewValue is not true)
         {
            return;
         }

         _ = window.CommandBindings.Add(new CommandBinding(
            SystemCommands.CloseWindowCommand,
            (_, _) => SystemCommands.CloseWindow(window)));

         _ = window.CommandBindings.Add(new CommandBinding(
            SystemCommands.MinimizeWindowCommand,
            (_, _) => SystemCommands.MinimizeWindow(window),
            (_, args) => args.CanExecute = window.ResizeMode != ResizeMode.NoResize));

         _ = window.CommandBindings.Add(new CommandBinding(
            SystemCommands.MaximizeWindowCommand,
            (_, _) => SystemCommands.MaximizeWindow(window),
            (_, args) => args.CanExecute = _canResize(window)));

         _ = window.CommandBindings.Add(new CommandBinding(
            SystemCommands.RestoreWindowCommand,
            (_, _) => SystemCommands.RestoreWindow(window),
            (_, args) => args.CanExecute = _canResize(window)));
      }

      private static bool _canResize(Window window)
         => window.ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip;
   }

   /// <summary>
   /// Title text for the custom title bar: the window <see cref="Window.Title"/> without
   /// the "Upsilon.Apps.Passkey.GUI.WPF vX.Y.Z" prefix, which the bar already shows.
   /// The Title property itself is unchanged (taskbar and Alt+Tab keep the full text).
   /// </summary>
   internal sealed class WindowTitleConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      {
         if (value is not string title)
         {
            return string.Empty;
         }

         if (!title.StartsWith(StrataChrome.TitlePrefix, StringComparison.Ordinal))
         {
            return title;
         }

         return title[StrataChrome.TitlePrefix.Length..].TrimStart(' ', '-').Trim();
      }

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
         => throw new NotSupportedException();
   }
}
