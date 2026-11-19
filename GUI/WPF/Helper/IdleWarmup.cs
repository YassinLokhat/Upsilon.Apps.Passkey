using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.WPF.Views.Controls;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Smooth the first time too: the first time a kind of control is built, WPF
   /// loads its styles and templates and the runtime compiles its code, which
   /// can cost tens of milliseconds on the first open of a window or drop-down.
   /// Once the first window has drawn, this builds one of each kind the app uses
   /// in a detached tree (nothing is shown, nothing is added to any window), one
   /// kind per idle moment so input and animation never wait, then lets it go.
   /// <para>
   /// It never touches the vault: only empty controls with placeholder text, no
   /// view model, no session, nothing that counts as activity.
   /// </para>
   /// </summary>
   internal static class IdleWarmup
   {
      private static bool _started;

      private static readonly Func<FrameworkElement>[] _kinds =
      [
         static () => new TextBox { Text = "warm" },
         static () => new CheckBox { Content = "warm" },
         static () => new RadioButton { Content = "warm" },
         static () => new Button { Content = "warm" },
         static () => new PasswordBox(),
         static () => new VisiblePasswordBox(),
         static () => new ComboBox { ItemsSource = new[] { "a", "b", "c" }, SelectedIndex = 0 },
         static () => new DatePicker(),
         static () => new Calendar(),
         static () => new ToolTip { Content = "warm" },
         static () => new ContextMenu { ItemsSource = new[] { new MenuItem { Header = "a" }, new MenuItem { Header = "b" } } },
         static () => new Menu { ItemsSource = new[] { new MenuItem { Header = "a" } } },
         static () => new DataGrid
         {
            AutoGenerateColumns = false,
            Columns = { new DataGridTextColumn { Header = "a", Binding = new System.Windows.Data.Binding() } },
            ItemsSource = new[] { "a", "b" },
         },
         static () => new PooledListBox { ItemsSource = new[] { "a", "b" } },
         static () => new ScrollViewer { Content = new TextBlock { Text = "warm" } },
      ];

      /// <summary>Starts once per run, after <paramref name="first"/> has drawn its first frame.</summary>
      public static void StartAfterFirstFrame(Window first)
      {
         ArgumentNullException.ThrowIfNull(first);
         if (_started)
         {
            return;
         }

         _started = true;
         first.ContentRendered += (_, _) => _next(first.Dispatcher, 0);
      }

      private static void _next(Dispatcher dispatcher, int index)
      {
         if (index >= _kinds.Length)
         {
            return;
         }

         _ = dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
         {
            try
            {
               _build(_kinds[index]());
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.Windows.Markup.XamlParseException)
            {
               // Warming is best effort: a kind that cannot be built detached is
               // simply built on its first real use, as before.
            }

            _next(dispatcher, index + 1);
         });
      }

      private static void _build(FrameworkElement element)
      {
         Border host = new() { Width = 400, Height = 300, Child = element };
         host.Measure(new Size(400, 300));
         host.Arrange(new Rect(0, 0, 400, 300));
         host.Child = null;
      }
   }
}
