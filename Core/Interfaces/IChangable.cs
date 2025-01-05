using Upsilon.Apps.Passkey.Core.Models;

namespace Upsilon.Apps.Passkey.Core.Interfaces
{
   internal interface IChangable
   {
      void Apply(Change change);
   }
}
