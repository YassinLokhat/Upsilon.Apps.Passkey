using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.Views.Controls
{
   /// <summary>
   /// Interaction logic for PrivateTextBox.xaml
   /// </summary>
   public partial class PrivateTextBox : UserControl
   {
      public enum PrivateTextBoxOffFocusDisplayBehavior
      {
         Classic = 0,
         Random = 1,
      }

      public enum PrivateTextBoxShowPrivateTextBehavior
      {
         ShowOnFocus = 0,
         ShowOnToogleButton = 1,
         ShowOnHoldButton = 2,
      }

      public PrivateTextBox()
      {
         InitializeComponent();

         DataContext = new PrivateTextBoxViewModel();
      }
   }
}
