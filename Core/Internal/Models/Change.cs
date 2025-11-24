namespace Upsilon.Apps.Passkey.Core.Internal.Models
{
   internal sealed class Change
   {
      public enum Type
      {
         None = 0,
         Update = 1,
         Add = 2,
         Delete = 3,
      }

      public long Index { get; set; } = long.MaxValue;
      public Type ActionType { get; set; } = Type.None;
      public string ItemId { get; set; } = string.Empty;
      public string FieldName { get; set; } = string.Empty;
      public string? OldValue { get; set; } = null;
      public string NewValue { get; set; } = string.Empty;
   }
}