using System.Buffers.Binary;
using System.Text;

namespace Upsilon.Apps.Passkey.Utils
{
   /// <summary>
   /// Windows NTLM password hash: MD4 of the UTF-16LE password bytes.
   /// Used by Have I Been Pwned's <c>?mode=ntlm</c> range API and the offline Bloom filter.
   /// </summary>
   internal static class NtlmHash
   {
      internal const int ByteLength = 16;

      /// <summary>
      /// Computes the NTLM digest of <paramref name="password"/> as uppercase hex (32 characters).
      /// </summary>
      internal static string HashHex(string password)
      {
         ArgumentNullException.ThrowIfNull(password);
         return Convert.ToHexString(Hash(password));
      }

      /// <summary>
      /// Computes the 16-byte NTLM digest of <paramref name="password"/>.
      /// </summary>
      internal static byte[] Hash(string password)
      {
         ArgumentNullException.ThrowIfNull(password);
         // Encoding.Unicode is UTF-16LE — the encoding Windows NTLM uses.
         return Md4.Hash(Encoding.Unicode.GetBytes(password));
      }

      /// <summary>
      /// RFC 1320 MD4. Intentionally weak; required by the NTLM / HIBP contract.
      /// </summary>
      private static class Md4
      {
         internal static byte[] Hash(ReadOnlySpan<byte> input)
         {
            uint a = 0x67452301;
            uint b = 0xEFCDAB89;
            uint c = 0x98BADCFE;
            uint d = 0x10325476;

            int bitLength = input.Length * 8;
            int paddedLength = ((input.Length + 8) / 64 + 1) * 64;
            Span<byte> block = paddedLength <= 128
               ? stackalloc byte[paddedLength]
               : new byte[paddedLength];
            block.Clear();
            input.CopyTo(block);
            block[input.Length] = 0x80;
            BinaryPrimitives.WriteUInt64LittleEndian(block[(paddedLength - 8)..], (ulong)bitLength);

            Span<uint> words = stackalloc uint[16];
            for (int offset = 0; offset < paddedLength; offset += 64)
            {
               for (int i = 0; i < 16; i++)
               {
                  words[i] = BinaryPrimitives.ReadUInt32LittleEndian(block.Slice(offset + (i * 4), 4));
               }

               uint aa = a, bb = b, cc = c, dd = d;

               // Round 1
               a = _ff(a, b, c, d, words[0], 3);
               d = _ff(d, a, b, c, words[1], 7);
               c = _ff(c, d, a, b, words[2], 11);
               b = _ff(b, c, d, a, words[3], 19);
               a = _ff(a, b, c, d, words[4], 3);
               d = _ff(d, a, b, c, words[5], 7);
               c = _ff(c, d, a, b, words[6], 11);
               b = _ff(b, c, d, a, words[7], 19);
               a = _ff(a, b, c, d, words[8], 3);
               d = _ff(d, a, b, c, words[9], 7);
               c = _ff(c, d, a, b, words[10], 11);
               b = _ff(b, c, d, a, words[11], 19);
               a = _ff(a, b, c, d, words[12], 3);
               d = _ff(d, a, b, c, words[13], 7);
               c = _ff(c, d, a, b, words[14], 11);
               b = _ff(b, c, d, a, words[15], 19);

               // Round 2
               a = _gg(a, b, c, d, words[0], 3);
               d = _gg(d, a, b, c, words[4], 5);
               c = _gg(c, d, a, b, words[8], 9);
               b = _gg(b, c, d, a, words[12], 13);
               a = _gg(a, b, c, d, words[1], 3);
               d = _gg(d, a, b, c, words[5], 5);
               c = _gg(c, d, a, b, words[9], 9);
               b = _gg(b, c, d, a, words[13], 13);
               a = _gg(a, b, c, d, words[2], 3);
               d = _gg(d, a, b, c, words[6], 5);
               c = _gg(c, d, a, b, words[10], 9);
               b = _gg(b, c, d, a, words[14], 13);
               a = _gg(a, b, c, d, words[3], 3);
               d = _gg(d, a, b, c, words[7], 5);
               c = _gg(c, d, a, b, words[11], 9);
               b = _gg(b, c, d, a, words[15], 13);

               // Round 3
               a = _hh(a, b, c, d, words[0], 3);
               d = _hh(d, a, b, c, words[8], 9);
               c = _hh(c, d, a, b, words[4], 11);
               b = _hh(b, c, d, a, words[12], 15);
               a = _hh(a, b, c, d, words[2], 3);
               d = _hh(d, a, b, c, words[10], 9);
               c = _hh(c, d, a, b, words[6], 11);
               b = _hh(b, c, d, a, words[14], 15);
               a = _hh(a, b, c, d, words[1], 3);
               d = _hh(d, a, b, c, words[9], 9);
               c = _hh(c, d, a, b, words[5], 11);
               b = _hh(b, c, d, a, words[13], 15);
               a = _hh(a, b, c, d, words[3], 3);
               d = _hh(d, a, b, c, words[11], 9);
               c = _hh(c, d, a, b, words[7], 11);
               b = _hh(b, c, d, a, words[15], 15);

               a += aa;
               b += bb;
               c += cc;
               d += dd;
            }

            byte[] digest = new byte[ByteLength];
            BinaryPrimitives.WriteUInt32LittleEndian(digest.AsSpan(0, 4), a);
            BinaryPrimitives.WriteUInt32LittleEndian(digest.AsSpan(4, 4), b);
            BinaryPrimitives.WriteUInt32LittleEndian(digest.AsSpan(8, 4), c);
            BinaryPrimitives.WriteUInt32LittleEndian(digest.AsSpan(12, 4), d);
            return digest;
         }

         private static uint _ff(uint a, uint b, uint c, uint d, uint x, int s)
            => uint.RotateLeft(a + ((b & c) | (~b & d)) + x, s);

         private static uint _gg(uint a, uint b, uint c, uint d, uint x, int s)
            => uint.RotateLeft(a + ((b & c) | (b & d) | (c & d)) + x + 0x5A827999u, s);

         private static uint _hh(uint a, uint b, uint c, uint d, uint x, int s)
            => uint.RotateLeft(a + (b ^ c ^ d) + x + 0x6ED9EBA1u, s);
      }
   }
}
