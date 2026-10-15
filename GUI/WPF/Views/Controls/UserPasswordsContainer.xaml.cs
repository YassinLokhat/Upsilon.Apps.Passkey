using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Settings;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views.Controls
{
   /// <summary>
   /// Interaction logic for UserPasswordsContainer.xaml: one <see cref="UserPasswordItem"/>
   /// per row of the bound <see cref="PasskeyListViewModel"/>, kept in its order.
   /// </summary>
   internal sealed partial class UserPasswordsContainer : UserControl
   {
      private readonly Dictionary<PasskeyEntryViewModel, UserPasswordItem> _items = [];
      private PasskeyListViewModel? _list;

      public UserPasswordsContainer()
      {
         InitializeComponent();
      }

      public void Bind(PasskeyListViewModel list)
      {
         ArgumentNullException.ThrowIfNull(list);
         _list = list;
         list.Entries.CollectionChanged += _entries_CollectionChanged;
         list.EntryAdded += (_, entry) => Dispatcher.BeginInvoke(() => _items.GetValueOrDefault(entry)?.Focus());
         _rebuild();
      }

      public void Unbind()
      {
         if (_list is not null)
         {
            _list.Entries.CollectionChanged -= _entries_CollectionChanged;
            _list = null;
         }

         _items.Clear();
         _stackPanel.Children.Clear();
      }

      private void _entries_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => _rebuild();

      private void _rebuild()
      {
         _stackPanel.Children.Clear();

         if (_list is null)
         {
            return;
         }

         foreach (PasskeyEntryViewModel entry in _list.Entries)
         {
            if (!_items.TryGetValue(entry, out UserPasswordItem? item))
            {
               item = new UserPasswordItem(entry);
               _items[entry] = item;
            }

            _ = _stackPanel.Children.Add(item);
         }

         foreach (PasskeyEntryViewModel gone in _items.Keys.Except(_list.Entries).ToArray())
         {
            _ = _items.Remove(gone);
         }

         _stackPanel.UpdateLayout();
      }

      private void _addButton_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy() || _list is null)
         {
            return;
         }

         _ = _list.Add();
         Window.GetWindow(this).ComputeTabIndex();
      }
   }
}
