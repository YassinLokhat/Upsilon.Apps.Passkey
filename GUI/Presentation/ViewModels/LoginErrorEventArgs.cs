namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels
{
   /// <summary>A failed open or login, for hosts that report it in a dialog.</summary>
   internal sealed class LoginErrorEventArgs(string title, string message) : EventArgs
   {
      public string Title { get; } = title;

      public string Message { get; } = message;
   }
}
