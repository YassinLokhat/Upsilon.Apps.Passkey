namespace Upsilon.Apps.Passkey.GUI.MAUI.Adapters
{
   /// <summary>
   /// Modal page shown by <see cref="MauiDialogService.ShowAsync{TResult}"/>.
   /// <see cref="Completion"/> completes when the user confirms or cancels
   /// (<c>default</c> result); the service pops the page if it is still shown.
   /// </summary>
   internal interface IResultPage<TResult>
   {
      Task<TResult?> Completion { get; }
   }
}
