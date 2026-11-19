using FluentAssertions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;

namespace Upsilon.Apps.Passkey.UnitTests.Windows.Gui
{
   /// <summary>
   /// A switch rebinds the rows already built instead of building new ones, and a
   /// row always hears when it gets and loses an item (so a password set from code
   /// is filled for its own item and wiped when it leaves).
   /// </summary>
   [STATestClass]
   [DoNotParallelize]
   public sealed class PooledListBoxTests
   {
      private HwndSource? _source;

      [TestCleanup]
      public void Cleanup() => _source?.Dispose();

      [TestMethod]
      public void ReplacedList_ReusesTheRowsAlreadyBuilt()
      {
         PooledListBox list = _host(recycling: false);

         list.ItemsSource = _items("a", 3);
         _layout();
         _ = list.BuiltCount.Should().Be(3);
         HashSet<TextBlock> built = [.. _blocks(list)];

         list.ItemsSource = _items("b", 3);
         _layout();

         _ = list.BuiltCount.Should().Be(3, "the second list rebinds the three rows built for the first");
         _ = _texts(list).Should().Equal("b0", "b1", "b2");
         _ = _blocks(list).Should().OnlyContain(x => built.Contains(x), "a reused row keeps its built template, not only its shell");
      }

      [TestMethod]
      public void LongerList_BuildsOnlyTheMissingRows()
      {
         PooledListBox list = _host(recycling: false);
         list.ItemsSource = _items("a", 2);
         _layout();

         list.ItemsSource = _items("b", 5);
         _layout();

         _ = list.BuiltCount.Should().Be(5);
         _ = _texts(list).Should().Equal("b0", "b1", "b2", "b3", "b4");
      }

      [TestMethod]
      public void EveryRow_HearsItGetsAndLosesItsItem()
      {
         PooledListBox list = _host(recycling: false);
         Dictionary<ListBoxItem, object?> holds = [];
         list.ContainerPrepared += (row, item) => holds[row] = item;
         list.ContainerCleared += row => holds[row] = null;

         list.ItemsSource = _items("a", 2);
         _layout();
         list.ItemsSource = _items("b", 1);
         _layout();

         _ = holds.Values.Where(x => x is not null).Should().Equal(["b0"], "the row left over holds nothing");
      }

      [TestMethod]
      public void RecyclingList_NeverHandsOutARowTwice()
      {
         PooledListBox list = _host(recycling: true);
         list.ItemsSource = _items("a", 4);
         _layout();

         list.ItemsSource = _items("b", 4);
         _layout();
         list.ItemsSource = _items("c", 4);
         _layout();

         _ = list.PooledCount.Should().Be(0, "a recycling list keeps its own rows");
         ListBoxItem[] rows = [.. Enumerable.Range(0, 4).Select(i => (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(i))];
         _ = rows.Should().OnlyHaveUniqueItems();
         _ = _texts(list).Should().Equal("c0", "c1", "c2", "c3");
      }

      private PooledListBox _host(bool recycling)
      {
         PooledListBox list = new()
         {
            Width = 200,
            Height = 400,
            ItemTemplate = new DataTemplate { VisualTree = new FrameworkElementFactory(typeof(TextBlock)) },
         };
         list.ItemTemplate.VisualTree.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding());
         VirtualizingPanel.SetIsVirtualizing(list, true);
         VirtualizingPanel.SetVirtualizationMode(list, recycling ? VirtualizationMode.Recycling : VirtualizationMode.Standard);

         // Hidden: no window is shown.
         _source = new HwndSource(new HwndSourceParameters(nameof(PooledListBoxTests)) { Width = 200, Height = 400, WindowStyle = 0 })
         {
            RootVisual = list,
         };
         return list;
      }

      private static List<string> _items(string prefix, int count) => [.. Enumerable.Range(0, count).Select(i => $"{prefix}{i}")];

      private static IEnumerable<string> _texts(ListBox list)
      {
         for (int i = 0; i < list.Items.Count; i++)
         {
            if (list.ItemContainerGenerator.ContainerFromIndex(i) is ListBoxItem row && PooledListBox.Find<TextBlock>(row) is TextBlock text)
            {
               yield return text.Text;
            }
         }
      }

      private static IEnumerable<TextBlock> _blocks(ListBox list)
      {
         for (int i = 0; i < list.Items.Count; i++)
         {
            if (list.ItemContainerGenerator.ContainerFromIndex(i) is ListBoxItem row && PooledListBox.Find<TextBlock>(row) is TextBlock text)
            {
               yield return text;
            }
         }
      }

      private static void _layout()
      {
         Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, () => { });
      }
   }
}
