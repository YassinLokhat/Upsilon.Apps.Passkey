using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Keeps an <see cref="ItemsControl"/>'s rows when its list is replaced by a new
   /// one of the same shape (the quality ticks on every account switch). Setting
   /// <c>helper:StableItems.Source</c> instead of <c>ItemsSource</c> gives the
   /// control one lasting collection of <see cref="Slot"/>s; a new list only
   /// updates <see cref="Slot.Value"/> in place (and adds or removes slots at the
   /// end), so the rows are never re-created, re-templated or re-measured from
   /// scratch. Templates bind through <c>Value</c> (<c>{Binding Value.Text}</c>).
   /// </summary>
   internal static class StableItems
   {
      public static readonly DependencyProperty SourceProperty = DependencyProperty.RegisterAttached(
         "Source", typeof(IEnumerable), typeof(StableItems), new PropertyMetadata(null, _onSourceChanged));

      public static IEnumerable? GetSource(DependencyObject element) => (IEnumerable?)element.GetValue(SourceProperty);

      public static void SetSource(DependencyObject element, IEnumerable? value) => element.SetValue(SourceProperty, value);

      private static readonly DependencyProperty _slotsProperty = DependencyProperty.RegisterAttached(
         "Slots", typeof(ObservableCollection<Slot>), typeof(StableItems), new PropertyMetadata(null));

      private static void _onSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
      {
         if (d is not ItemsControl items)
         {
            return;
         }

         if (items.GetValue(_slotsProperty) is not ObservableCollection<Slot> slots)
         {
            slots = [];
            items.SetValue(_slotsProperty, slots);
            items.ItemsSource = slots;
         }

         Update(slots, e.NewValue as IEnumerable);
      }

      /// <summary>Brings <paramref name="slots"/> in line with <paramref name="source"/>, reusing slots in place.</summary>
      internal static void Update(ObservableCollection<Slot> slots, IEnumerable? source)
      {
         ArgumentNullException.ThrowIfNull(slots);

         int index = 0;
         if (source is not null)
         {
            foreach (object? value in source)
            {
               if (index < slots.Count)
               {
                  slots[index].Value = value;
               }
               else
               {
                  slots.Add(new Slot { Value = value });
               }

               index++;
            }
         }

         while (slots.Count > index)
         {
            slots.RemoveAt(slots.Count - 1);
         }
      }

      /// <summary>One lasting row; only its value changes.</summary>
      internal sealed class Slot : INotifyPropertyChanged
      {
         private static readonly PropertyChangedEventArgs _valueChanged = new(nameof(Value));

         public event PropertyChangedEventHandler? PropertyChanged;

         public object? Value
         {
            get;
            set
            {
               if (Equals(field, value))
               {
                  return;
               }

               field = value;
               PropertyChanged?.Invoke(this, _valueChanged);
            }
         }
      }
   }
}
