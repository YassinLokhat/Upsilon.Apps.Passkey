namespace Upsilon.Apps.Passkey.Core.Interfaces
{
   public interface IAccount : IItem
   {
      string Label { get; set; }
      string Notes { get; set; }
      string[] Identifiants { get; set; }
      string Password { get; set; }
      Dictionary<long, string> Passwords { get; }
      Models.AccountOption Options { get; set; }
      IService Service { get; }
   }
}