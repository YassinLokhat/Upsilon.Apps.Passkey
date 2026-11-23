using FluentAssertions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;

namespace Upsilon.Apps.Passkey.UnitTests.Windows.Gui
{
   /// <summary>
   /// A held arrow key moves the list at every step but fills the details pane
   /// only for the row the keys stop on; a single press and a click fill it at once;
   /// nothing else may be delayed or lost.
   /// </summary>
   [STATestClass]
   [DoNotParallelize]
   public sealed class HeldKeySelectionTests
   {
      private HwndSource? _source;
      private ListBox? _list;
      private HeldKeySelection? _heldKeys;
      private readonly List<object?> _filled = [];
      private bool _busy;

      [TestInitialize]
      public void Initialize()
      {
         // A hidden input source for the synthetic key events: no window is shown.
         _source = new HwndSource(new HwndSourceParameters(nameof(HeldKeySelectionTests)) { Width = 0, Height = 0, WindowStyle = 0 });
         _list = new ListBox { ItemsSource = Enumerable.Range(0, 20).ToList() };
         _heldKeys = new HeldKeySelection(_list, () =>
         {
            if (_busy)
            {
               return false;
            }

            _filled.Add(_list.SelectedItem);
            return true;
         });
         _list.SelectionChanged += (_, _) =>
         {
            if (!_heldKeys.Defer())
            {
               _filled.Add(_list.SelectedItem);
            }
         };
         _filled.Clear();
         _busy = false;
      }

      [TestCleanup]
      public void Cleanup()
      {
         _heldKeys!.Cancel();
         _source!.Dispose();
      }

      [TestMethod]
      public void SinglePress_FillsAtOnce()
      {
         _step(Key.Down, 1);

         _ = _filled.Should().Equal(1);
      }

      [TestMethod]
      public void SelectionByCode_FillsAtOnce()
      {
         _list!.SelectedIndex = 3;

         _ = _filled.Should().Equal(3);
      }

      [TestMethod]
      public void HeldKey_FillsOnlyTheRowItStopsOn_WhenReleased()
      {
         _step(Key.Down, 1);
         for (int i = 2; i <= 8; i++)
         {
            _step(Key.Down, i);
         }

         _ = _filled.Should().Equal([1], "the repeated steps only move the list");

         _raise(Keyboard.KeyUpEvent, Key.Down);

         _ = _filled.Should().Equal(1, 8);
      }

      [TestMethod]
      public void HeldKey_FillsAfterAShortPause()
      {
         _step(Key.Down, 1);
         _step(Key.Down, 2);
         _step(Key.Down, 3);

         _pump(TimeSpan.FromMilliseconds(250));

         _ = _filled.Should().Equal(1, 3);
      }

      [TestMethod]
      public void OtherKey_FillsBeforeItActs()
      {
         _step(Key.Down, 1);
         _step(Key.Down, 2);

         _raise(Keyboard.PreviewKeyDownEvent, Key.Delete);

         _ = _filled.Should().Equal(1, 2);
      }

      [TestMethod]
      public void Busy_RetriesInsteadOfLeavingAnOlderRow()
      {
         _step(Key.Down, 1);
         _step(Key.Down, 2);
         _busy = true;

         _raise(Keyboard.KeyUpEvent, Key.Down);
         _ = _filled.Should().Equal(1);

         _busy = false;
         _pump(TimeSpan.FromMilliseconds(250));

         _ = _filled.Should().Equal(1, 2);
      }

      [TestMethod]
      public void Cancel_DropsTheWaitingFill()
      {
         _step(Key.Down, 1);
         _step(Key.Down, 2);

         _heldKeys!.Cancel();
         _pump(TimeSpan.FromMilliseconds(250));

         _ = _filled.Should().Equal(1);
      }

      [TestMethod]
      public void LostKeyDown_DoesNotDeferTheNextSelection()
      {
         _step(Key.Down, 1);
         // A second step whose KeyDown never reaches the list.
         _raise(Keyboard.PreviewKeyDownEvent, Key.Down);
         _raise(Keyboard.KeyUpEvent, Key.Down);

         _list!.SelectedIndex = 5;

         _ = _filled.Should().Equal(1, 5);
      }

      // One key step as the list sees it: preview, the list's own move, then KeyDown.
      private void _step(Key key, int index)
      {
         _raise(Keyboard.PreviewKeyDownEvent, key);
         _list!.SelectedIndex = index;
         _raise(Keyboard.KeyDownEvent, key);
      }

      private void _raise(RoutedEvent routedEvent, Key key)
      {
         _list!.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, _source!, Environment.TickCount, key) { RoutedEvent = routedEvent });
      }

      private static void _pump(TimeSpan duration)
      {
         DispatcherFrame frame = new();
         DispatcherTimer stop = new(DispatcherPriority.Background) { Interval = duration };
         stop.Tick += (_, _) =>
         {
            stop.Stop();
            frame.Continue = false;
         };
         stop.Start();
         Dispatcher.PushFrame(frame);
      }
   }
}
