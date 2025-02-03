namespace Upsilon.Apps.PassKey.Core.Implementation.Models
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

      public Type ActionType { get; set; } = Type.None;
      public string ItemId { get; set; } = string.Empty;
      public string FieldName { get; set; } = string.Empty;
      public string Value { get; set; } = string.Empty;
   }
}