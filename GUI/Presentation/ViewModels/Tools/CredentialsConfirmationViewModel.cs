using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;

namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Tools
{
   /// <summary>
   /// Progressive credential re-entry for create / update / delete / open User Settings.
   /// Mistakes intentionally poison the in-dialog sequence until Escape resets it
   /// (same no-rollback rule as progressive login — not a UX defect).
   /// <para>
   /// Expected factors are kept only as HMAC-SHA256 digests under a random
   /// per-dialog key: no plaintext is retained, and the fixed-time digest
   /// comparison does not depend on the factor lengths.
   /// </para>
   /// </summary>
   internal sealed class CredentialsConfirmationViewModel : ObservableObject
   {
      private readonly bool _isNew;
      private byte[]? _key;
      private byte[][]? _expected;
      private int _acceptedCount;

      /// <summary>
      /// Set on the first mismatched factor until <see cref="ClearCredentials"/> (Escape).
      /// </summary>
      private bool _poisoned;

      public CredentialsConfirmationViewModel(IEnumerable<string> credentials, bool isNew)
      {
         ArgumentNullException.ThrowIfNull(credentials);
         _isNew = isNew;
         _key = RandomNumberGenerator.GetBytes(32);
         _expected = [.. credentials.Select(x => _digest(_key, x))];
         Title = _title();
      }

      public string Title
      {
         get;
         private set => SetProperty(ref field, value);
      }

      public bool IsAwaitingPasskeys
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(CredentialsLabel));
            }
         }
      }

      public string CredentialsLabel => IsAwaitingPasskeys ? Strings.Label_Password : Strings.Label_Username;

      public void OnLanguageChanged()
      {
         Title = _title();
         OnPropertyChanged(nameof(CredentialsLabel));
      }

      /// <summary>
      /// Clears poison and accepted count so the sequence can restart (Escape — not dialog cancel).
      /// </summary>
      public void ClearCredentials()
      {
         _acceptedCount = 0;
         _poisoned = false;
         IsAwaitingPasskeys = false;
      }

      /// <summary>Wipes the digests and key once the dialog closes; later input never matches.</summary>
      public void ReleaseExpectedCredentials()
      {
         if (_expected is null)
         {
            return;
         }

         foreach (byte[] digest in _expected)
         {
            CryptographicOperations.ZeroMemory(digest);
         }

         CryptographicOperations.ZeroMemory(_key);
         _expected = null;
         _key = null;
         ClearCredentials();
      }

      /// <summary>
      /// Accepts the next typed factor. A mismatch poisons until Escape; typed factors are not retained.
      /// Feedback is identical for a right or wrong factor until the last one.
      /// </summary>
      public bool ValidateCredentials(string? credential)
      {
         byte[][]? expected = _expected;
         byte[]? key = _key;
         IsAwaitingPasskeys = true;
         if (expected is null || key is null || _poisoned || credential is null)
         {
            _poisoned |= expected is not null;
            return false;
         }

         int index = _acceptedCount;
         byte[] typed = _digest(key, credential);
         try
         {
            if (index >= expected.Length
               || !CryptographicOperations.FixedTimeEquals(typed, expected[index]))
            {
               _poisoned = true;
               return false;
            }
         }
         finally
         {
            CryptographicOperations.ZeroMemory(typed);
         }

         _acceptedCount++;
         return _acceptedCount == expected.Length;
      }

      private string _title()
         => _isNew ? Strings.Title_NewCredentialsConfirmation : Strings.Title_ActualCredentialsConfirmation;

      private static byte[] _digest(byte[] key, string value)
      {
         int length = Encoding.UTF8.GetByteCount(value);
         byte[] buffer = ArrayPool<byte>.Shared.Rent(Math.Max(length, 1));
         try
         {
            int written = Encoding.UTF8.GetBytes(value, buffer);
            return HMACSHA256.HashData(key, buffer.AsSpan(0, written));
         }
         finally
         {
            CryptographicOperations.ZeroMemory(buffer);
            ArrayPool<byte>.Shared.Return(buffer);
         }
      }
   }
}
