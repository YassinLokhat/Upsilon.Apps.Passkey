using Upsilon.Apps.PassKey.Core.Implementation.Models;

namespace Upsilon.Apps.PassKey.Core.Implementation.Interfaces
{
   internal interface IChangable
   {
      void Apply(Change change);
   }
}
