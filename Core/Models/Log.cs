using Upsilon.Apps.PassKey.Core.Interfaces;

namespace Upsilon.Apps.PassKey.Core.Models
{
   internal class Log : ILog
   {
      DateTime ILog.DateTime => throw new NotImplementedException();

      string ILog.ItemId => throw new NotImplementedException();

      string ILog.Message => throw new NotImplementedException();
   }
}
