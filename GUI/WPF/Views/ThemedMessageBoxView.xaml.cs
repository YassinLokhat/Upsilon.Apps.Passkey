using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Themed replacement for <see cref="MessageBox"/> that follows application
   /// light/dark resources instead of the native Win32 dialog chrome.
   /// </summary>
   internal sealed partial class ThemedMessageBoxView : Window
   {
      public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

      private ThemedMessageBoxView()
      {
         InitializeComponent();
         Loaded += (_, _) => this.PostLoadSetup();
      }

      public static MessageBoxResult Show(
         Window? owner,
         string text,
         string title,
         MessageBoxButton buttons = MessageBoxButton.OK,
         MessageBoxImage image = MessageBoxImage.None)
      {
         ThemedMessageBoxView dialog = new()
         {
            Owner = owner,
            Title = title,
         };

         if (owner is null)
         {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
         }

         dialog._messageText.Text = text.Trim();
         dialog._configureIcon(image);
         dialog._configureButtons(buttons);
         _ = dialog.ShowDialog();
         return dialog.Result;
      }

      private void _configureIcon(MessageBoxImage image)
      {
         if (image == MessageBoxImage.None)
         {
            _iconText.Visibility = Visibility.Collapsed;
            return;
         }

         (_iconText.Text, Brush brush) = image switch
         {
            MessageBoxImage.Information => ("i", _brush("InfoBrush")),
            MessageBoxImage.Warning => ("!", _brush("WarningBrush")),
            MessageBoxImage.Error => ("✕", _brush("DangerBrush")),
            MessageBoxImage.Question => ("?", _brush("AccentBrush")),
            _ => (string.Empty, _brush("ForegroundBrush")),
         };

         if (string.IsNullOrEmpty(_iconText.Text))
         {
            _iconText.Visibility = Visibility.Collapsed;
            return;
         }

         _iconText.Foreground = brush;
         _iconText.Visibility = Visibility.Visible;
      }

      private void _configureButtons(MessageBoxButton buttons)
      {
         _buttonsPanel.Children.Clear();

         switch (buttons)
         {
            case MessageBoxButton.OK:
               _addButton(Strings.Button_OK, MessageBoxResult.OK, isDefault: true, isCancel: true);
               break;

            case MessageBoxButton.OKCancel:
               _addButton(Strings.Button_OK, MessageBoxResult.OK, isDefault: true);
               _addButton(Strings.Button_Cancel, MessageBoxResult.Cancel, isCancel: true);
               break;

            case MessageBoxButton.YesNo:
               _addButton(Strings.Button_Yes, MessageBoxResult.Yes, isDefault: true);
               _addButton(Strings.Button_No, MessageBoxResult.No, isCancel: true);
               break;

            case MessageBoxButton.YesNoCancel:
               _addButton(Strings.Button_Yes, MessageBoxResult.Yes, isDefault: true);
               _addButton(Strings.Button_No, MessageBoxResult.No);
               _addButton(Strings.Button_Cancel, MessageBoxResult.Cancel, isCancel: true);
               break;
         }
      }

      private void _addButton(
         string label,
         MessageBoxResult result,
         bool isDefault = false,
         bool isCancel = false)
      {
         Button button = new()
         {
            Content = label,
            MinWidth = 75,
            IsDefault = isDefault,
            IsCancel = isCancel,
         };

         button.Click += (_, _) => _closeWith(result);
         _ = _buttonsPanel.Children.Add(button);
      }

      private void _closeWith(MessageBoxResult result)
      {
         Result = result;
         DialogResult = true;
      }

      private static Brush _brush(string key)
         => Application.Current?.TryFindResource(key) as Brush
            ?? Brushes.White;
   }
}
