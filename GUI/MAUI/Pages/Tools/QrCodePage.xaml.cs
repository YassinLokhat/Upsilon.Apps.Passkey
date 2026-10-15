using Upsilon.Apps.Passkey.GUI.MAUI.Adapters;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Tools;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Pages.Tools
{
   internal sealed partial class QrCodePage : ContentPage, IResultPage<bool>
   {
      private readonly QrCodeViewModel _viewModel;
      private readonly TaskCompletionSource<bool> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

      public QrCodePage(string content, int delayMilliseconds)
      {
         InitializeComponent();
         _viewModel = new QrCodeViewModel(content, delayMilliseconds);
         Title = _viewModel.Title;
         QrView.Drawable = new QrDrawable(_viewModel);
         _viewModel.Expired += (s, e) => _complete();
      }

      public Task<bool> Completion => _completion.Task;

      protected override bool OnBackButtonPressed()
      {
         _complete();
         return true;
      }

      protected override void OnDisappearing()
      {
         base.OnDisappearing();
         _complete();
      }

      private void _onClose(object? sender, EventArgs e) => _complete();

      private void _complete()
      {
         _viewModel.Close();
         QrView.Invalidate();
         _ = _completion.TrySetResult(true);
      }

      private sealed class QrDrawable(QrCodeViewModel viewModel) : IDrawable
      {
         public void Draw(ICanvas canvas, RectF dirtyRect)
         {
            bool[][] modules = viewModel.Modules;
            if (modules.Length == 0)
            {
               return;
            }

            float unit = Math.Min(dirtyRect.Width, dirtyRect.Height) / (modules.Length + 2);
            float left = dirtyRect.Left + ((dirtyRect.Width - (unit * (modules.Length + 2))) / 2);
            float top = dirtyRect.Top + ((dirtyRect.Height - (unit * (modules.Length + 2))) / 2);
            canvas.FillColor = Colors.Black;

            for (int i = 0; i < modules.Length; i++)
            {
               for (int j = 0; j < modules[i].Length; j++)
               {
                  if (modules[i][j])
                  {
                     canvas.FillRectangle(left + ((i + 1) * unit), top + ((j + 1) * unit), unit, unit);
                  }
               }
            }
         }
      }
   }
}
