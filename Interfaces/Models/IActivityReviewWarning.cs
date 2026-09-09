namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   public interface IActivityReviewWarning : IWarning
   {
      IEnumerable<IActivity> Activities { get; }
   }
}
