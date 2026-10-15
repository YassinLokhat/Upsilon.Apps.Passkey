using Upsilon.Apps.Passkey.GUI.MAUI.Adapters;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Pages
{
   /// <summary>Modal page completing with <c>true</c> when closed by any route (button, back, pop).</summary>
   internal abstract class ClosablePage : ContentPage, IResultPage<bool>
   {
      private readonly TaskCompletionSource<bool> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

      public Task<bool> Completion => _completion.Task;

      protected override bool OnBackButtonPressed()
      {
         CompletePage();
         return true;
      }

      protected override void OnDisappearing()
      {
         base.OnDisappearing();
         CompletePage();
      }

      protected void CompletePage()
      {
         if (_completion.TrySetResult(true))
         {
            OnCompleted();
         }
      }

      protected virtual void OnCompleted()
      {
      }
   }
}
