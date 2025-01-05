using Upsilon.Apps.PassKey.Core.Enums;

namespace Upsilon.Apps.Passkey.Core.Models
{
   internal sealed class Change
   {
      public ChangeType ActionType { get; set; } = ChangeType.None;
      public string ItemId { get; set; } = string.Empty;
      public string FieldName { get; set; } = string.Empty;
      public string Value { get; set; } = string.Empty;
   }
}