namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   /// <summary>State of one password-quality tick in the password core.</summary>
   internal enum QualityState
   {
      /// <summary>The check passes (accent tick).</summary>
      Pass,

      /// <summary>The check fails (warning cross).</summary>
      Fail,

      /// <summary>The check is switched off for this account (muted dash).</summary>
      Off,
   }

   /// <summary>One line of the password core's quality list ("12 or more characters").</summary>
   internal sealed record QualityCheck(string Text, QualityState State);
}
