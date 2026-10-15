namespace Upsilon.Apps.Passkey.GUI.Presentation.Models
{
   /// <summary>
   /// Typed request to show a host view (WPF window, MAUI page) and await its
   /// result. Each host maps the concrete request type to its own view; the
   /// result is <c>default</c> when the view is cancelled or closed.
   /// </summary>
   /// <typeparam name="TResult">Value returned when the view closes.</typeparam>
#pragma warning disable CA1040 // Marker interface: the generic parameter carries the contract
   internal interface IViewRequest<TResult>
   {
   }
#pragma warning restore CA1040
}
