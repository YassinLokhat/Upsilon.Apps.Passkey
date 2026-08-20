using System.Text;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   /// <summary>
   /// Original Keccak-512 (padding <c>0x01</c>), as required by XposedOrNot's
   /// anonymous password API. This is deliberately <em>not</em> NIST SHA-3-512
   /// (<c>SHA3_512</c> / padding <c>0x06</c>), whose digests differ.
   /// </summary>
   internal static class Keccak512
   {
      // Keccak-512: capacity 1024 → rate 576 bits = 72 bytes; digest 64 bytes.
      private const int RATE_BYTES = 72;
      private const int OUTPUT_BYTES = 64;

      private static readonly ulong[] _roundConstants =
      [
         0x0000000000000001UL, 0x0000000000008082UL, 0x800000000000808aUL, 0x8000000080008000UL,
         0x000000000000808bUL, 0x0000000080000001UL, 0x8000000080008081UL, 0x8000000000008009UL,
         0x000000000000008aUL, 0x0000000000000088UL, 0x0000000080008009UL, 0x000000008000000aUL,
         0x000000008000808bUL, 0x800000000000008bUL, 0x8000000000008089UL, 0x8000000000008003UL,
         0x8000000000008002UL, 0x8000000000000080UL, 0x000000000000800aUL, 0x800000008000000aUL,
         0x8000000080008081UL, 0x8000000000008080UL, 0x0000000080000001UL, 0x8000000080008008UL,
      ];

      // Rotation offsets indexed as x + 5*y for the rho step.
      private static readonly int[] _rotationOffsets =
      [
         0, 1, 62, 28, 27,
         36, 44, 6, 55, 20,
         3, 10, 43, 25, 39,
         41, 45, 15, 21, 8,
         18, 2, 61, 56, 14,
      ];

      /// <summary>
      /// Computes the Keccak-512 digest of <paramref name="utf8Text"/> and
      /// returns it as uppercase hexadecimal (128 characters).
      /// </summary>
      internal static string HashHex(string utf8Text)
      {
         ArgumentNullException.ThrowIfNull(utf8Text);
         return Convert.ToHexString(Hash(Encoding.UTF8.GetBytes(utf8Text)));
      }

      internal static byte[] Hash(ReadOnlySpan<byte> input)
      {
         Span<ulong> state = stackalloc ulong[25];
         state.Clear();

         int offset = 0;
         while (offset + RATE_BYTES <= input.Length)
         {
            _absorb(state, input.Slice(offset, RATE_BYTES));
            _keccakF(state);
            offset += RATE_BYTES;
         }

         Span<byte> block = stackalloc byte[RATE_BYTES];
         block.Clear();
         int remaining = input.Length - offset;
         input[offset..].CopyTo(block);
         // Multi-rate padding for raw Keccak (not SHA-3 domain separation).
         block[remaining] = 0x01;
         block[RATE_BYTES - 1] |= 0x80;
         _absorb(state, block);
         _keccakF(state);

         byte[] output = new byte[OUTPUT_BYTES];
         for (int i = 0; i < OUTPUT_BYTES; i++)
         {
            output[i] = (byte)(state[i / 8] >> (8 * (i % 8)));
         }

         return output;
      }

      private static void _absorb(Span<ulong> state, ReadOnlySpan<byte> block)
      {
         for (int i = 0; i < RATE_BYTES; i++)
         {
            state[i / 8] ^= (ulong)block[i] << (8 * (i % 8));
         }
      }

      private static void _keccakF(Span<ulong> state)
      {
         Span<ulong> c = stackalloc ulong[5];
         Span<ulong> d = stackalloc ulong[5];
         Span<ulong> b = stackalloc ulong[25];

         for (int round = 0; round < 24; round++)
         {
            _keccakRoundPart1(state, b, c, d, round);
            _keccakRoundPart2(state, b, c, d, round);
         }
      }

      private static void _keccakRoundPart1(Span<ulong> state, Span<ulong> b, Span<ulong> c, Span<ulong> d, int round)
      {
         for (int x = 0; x < 5; x++)
         {
            c[x] = state[x] ^ state[x + 5] ^ state[x + 10] ^ state[x + 15] ^ state[x + 20];
         }

         for (int x = 0; x < 5; x++)
         {
            d[x] = c[(x + 4) % 5] ^ _rotl(c[(x + 1) % 5], 1);
         }

         for (int i = 0; i < 25; i++)
         {
            state[i] ^= d[i % 5];
         }
      }

      private static void _keccakRoundPart2(Span<ulong> state, Span<ulong> b, Span<ulong> c, Span<ulong> d, int round)
      {
         for (int x = 0; x < 5; x++)
         {
            for (int y = 0; y < 5; y++)
            {
               int index = x + (5 * y);
               int newX = y;
               int newY = ((2 * x) + (3 * y)) % 5;
               b[newX + (5 * newY)] = _rotl(state[index], _rotationOffsets[index]);
            }
         }

         for (int x = 0; x < 5; x++)
         {
            for (int y = 0; y < 5; y++)
            {
               int index = x + (5 * y);
               state[index] = b[index] ^ (~b[((x + 1) % 5) + (5 * y)] & b[((x + 2) % 5) + (5 * y)]);
            }
         }

         state[0] ^= _roundConstants[round];
      }

      private static ulong _rotl(ulong value, int bits)
         => bits == 0 ? value : (value << bits) | (value >> (64 - bits));
   }
}
