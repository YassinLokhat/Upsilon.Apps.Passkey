using Upsilon.Apps.Passkey.GUI.MAUI.Adapters;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Tools;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Pages.Tools
{
   internal sealed partial class InsertIdentifierPage : ContentPage, IResultPage<IIdentifier>
   {
      private readonly InsertIdentifierViewModel _viewModel;
      private readonly IdentifierType[] _types;
      private readonly TaskCompletionSource<IIdentifier?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

      public InsertIdentifierPage(IEnumerable<IIdentifier> identifiers, IIdentifier? current)
      {
         InitializeComponent();
         BindingContext = _viewModel = new InsertIdentifierViewModel(identifiers, current);

         _types = [InsertIdentifierViewModel.AllIdentifierType, .. Enum.GetValues<IdentifierType>()];
         TypePicker.ItemsSource = _types
            .Select(x => x == InsertIdentifierViewModel.AllIdentifierType
               ? Strings.IdentifierType_All
               : Strings.Get($"IdentifierType_{x}"))
            .ToList();
         TypePicker.SelectedIndex = Array.IndexOf(_types, _viewModel.Type);
      }

      public Task<IIdentifier?> Completion => _completion.Task;

      protected override bool OnBackButtonPressed()
      {
         _ = _completion.TrySetResult(null);
         return true;
      }

      protected override void OnDisappearing()
      {
         base.OnDisappearing();
         _ = _completion.TrySetResult(null);
      }

      private void _onTypeChanged(object? sender, EventArgs e)
      {
         if (TypePicker.SelectedIndex >= 0)
         {
            _viewModel.Type = _types[TypePicker.SelectedIndex];
         }
      }

      private void _onCompleted(object? sender, EventArgs e)
      {
         if (!string.IsNullOrWhiteSpace(_viewModel.Identifier))
         {
            _ = _completion.TrySetResult(_viewModel.ToIdentifier());
         }
      }

      private void _onSelected(object? sender, SelectionChangedEventArgs e)
      {
         if (e.CurrentSelection.Count > 0 && e.CurrentSelection[0] is string value)
         {
            _viewModel.Identifier = value;
            _ = _completion.TrySetResult(_viewModel.ToIdentifier());
         }
      }

      private void _onCancel(object? sender, EventArgs e) => _completion.TrySetResult(null);
   }
}
