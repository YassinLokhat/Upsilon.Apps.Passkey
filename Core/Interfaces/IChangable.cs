using Upsilon.Apps.PassKey.Core.Models;

namespace Upsilon.Apps.PassKey.Core.Interfaces
{
   internal interface IChangable
   {
      void Apply(Change change);
   }
}
