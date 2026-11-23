using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// A <see cref="ListBox"/> that keeps the rows it built when its list is replaced
   /// (a new account, a new service) and hands them out again for the new items,
   /// so a switch rebinds existing rows instead of building every row template
   /// again (an identifier row costs about 5 ms to build, rebinding a fraction).
   /// <para>
   /// <see cref="ContainerPrepared"/> is raised whenever a row is given an item,
   /// a new row or a reused one, and <see cref="ContainerCleared"/> whenever a row
   /// lets go of one: rows whose content is set from code (a password box) fill
   /// it on the first and wipe it on the second, so a reused row never shows,
   /// or keeps, another item's secret.
   /// </para>
   /// <para>
   /// Rows are kept only for a list that does not recycle its own (virtualization
   /// mode Recycling keeps them itself); a kept row is reused only once it has
   /// left the tree.
   /// </para>
   /// </summary>
   internal class PooledListBox : ListBox
   {
      private const int MAX_POOLED = 48;
      private readonly Stack<ListBoxItem> _pool = new();

      public PooledListBox()
      {
         // Implicit styles are keyed by exact type: take the ListBox one.
         SetResourceReference(StyleProperty, typeof(ListBox));
      }

      /// <summary>A row was given <c>item</c> (new or reused). Its template may not be applied yet on a new row.</summary>
      public event Action<ListBoxItem, object>? ContainerPrepared;

      /// <summary>A row let go of its item (it leaves the list, is recycled or is kept for reuse).</summary>
      public event Action<ListBoxItem>? ContainerCleared;

      /// <summary>Rows kept for reuse right now (tests).</summary>
      internal int PooledCount => _pool.Count;

      /// <summary>Rows built from scratch so far (tests).</summary>
      internal int BuiltCount { get; private set; }

      private bool _poolsRows => !VirtualizingPanel.GetIsVirtualizing(this)
         || VirtualizingPanel.GetVirtualizationMode(this) != VirtualizationMode.Recycling;

      protected override DependencyObject GetContainerForItemOverride()
      {
         while (_pool.TryPop(out ListBoxItem? kept))
         {
            if (VisualTreeHelper.GetParent(kept) is null && LogicalTreeHelper.GetParent(kept) is null)
            {
               _forgetTyping(kept);
               return kept;
            }
         }

         BuiltCount++;
         return base.GetContainerForItemOverride();
      }

      protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
      {
         base.PrepareContainerForItemOverride(element, item);
         if (element is ListBoxItem row && !ReferenceEquals(element, item))
         {
            ContainerPrepared?.Invoke(row, item);
         }
      }

      protected override void ClearContainerForItemOverride(DependencyObject element, object item)
      {
         base.ClearContainerForItemOverride(element, item);
         if (element is not ListBoxItem row || ReferenceEquals(element, item))
         {
            return;
         }

         ContainerCleared?.Invoke(row);
         if (_poolsRows && _pool.Count < MAX_POOLED && !_pool.Contains(row))
         {
            _pool.Push(row);
         }
      }

      // A reused row's text boxes must not undo back to the previous item's text.
      private static void _forgetTyping(DependencyObject root)
      {
         int count = VisualTreeHelper.GetChildrenCount(root);
         for (int i = 0; i < count; i++)
         {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is System.Windows.Controls.Primitives.TextBoxBase { IsUndoEnabled: true } box)
            {
               box.IsUndoEnabled = false;
               box.IsUndoEnabled = true;
            }

            _forgetTyping(child);
         }
      }

      /// <summary>The first descendant of <paramref name="root"/> of type <typeparamref name="T"/>, or null (template not applied yet).</summary>
      internal static T? Find<T>(DependencyObject root) where T : DependencyObject
      {
         int count = VisualTreeHelper.GetChildrenCount(root);
         for (int i = 0; i < count; i++)
         {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
               return match;
            }

            if (Find<T>(child) is T found)
            {
               return found;
            }
         }

         return null;
      }
   }
}
