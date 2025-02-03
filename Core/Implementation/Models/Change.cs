using Upsilon.Apps.PassKey.Core.Implementation.Enums;

namespace Upsilon.Apps.PassKey.Core.Implementation.Models
{
   internal sealed class Change
   {
      public ChangeType ActionType { get; set; } = ChangeType.None;
      public string ItemId { get; set; } = string.Empty;
      public string FieldName { get; set; } = string.Empty;
      public string Value { get; set; } = string.Empty;
   }
}