namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   public interface IActivityReviewAlert : IAlert
   {
      IEnumerable<IActivity> Activities { get; }
   }
}
