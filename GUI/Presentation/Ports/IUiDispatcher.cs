namespace Upsilon.Apps.Passkey.GUI.Presentation.Ports
{
   /// <summary>Marshals work onto the UI thread.</summary>
   internal interface IUiDispatcher
   {
      void Post(Action action);

      void Send(Action action);

      T Send<T>(Func<T> func);

      bool CheckAccess();
   }
}
