using System.IO.MemoryMappedFiles;
using System.Text;

namespace Upsilon.Apps.Passkey.Utils.LeakFilter
{
   /// <summary>
   /// On-disk HIBP SHA-1 Bloom filter (<c>.pkbf</c>) backed by a memory-mapped bit array.
   /// </summary>
   internal sealed class HibpBloomFile : ILocalLeakFilter
   {
      internal const string Magic = "PKBF";
      internal const uint FormatVersion = 1;
      internal const int HeaderSize = 80;
      internal const int SourceTagBytes = 32;
      internal const int Sha1ByteLength = 20;
      internal const string DefaultSourceTag = "hibp-sha1";

      private readonly MemoryMappedFile _mmf;
      private readonly MemoryMappedViewAccessor _accessor;
      private readonly ulong _bitCount;
      private readonly int _hashFunctions;
      private readonly bool _writable;
      private ulong _insertedCount;
      private bool _disposed;

      private HibpBloomFile(
         MemoryMappedFile mmf,
         MemoryMappedViewAccessor accessor,
         string path,
         ulong capacity,
         ulong bitCount,
         int hashFunctions,
         ulong insertedCount,
         DateTime builtUtc,
         string sourceTag,
         bool writable)
      {
         _mmf = mmf;
         _accessor = accessor;
         Path = path;
         Capacity = capacity;
         _bitCount = bitCount;
         _hashFunctions = hashFunctions;
         _insertedCount = insertedCount;
         BuiltUtc = builtUtc;
         SourceTag = sourceTag;
         _writable = writable;
      }

      public string? Path { get; }

      public DateTime BuiltUtc { get; private set; }

      public ulong InsertedCount => _insertedCount;

      internal ulong Capacity { get; }

      internal ulong BitCount => _bitCount;

      internal int HashFunctions => _hashFunctions;

      internal string SourceTag { get; }

      /// <summary>
      /// Opens an existing <c>.pkbf</c> for read-only membership queries.
      /// </summary>
      internal static HibpBloomFile Open(string path)
      {
         ArgumentException.ThrowIfNullOrWhiteSpace(path);

         FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
         try
         {
            if (stream.Length < HeaderSize)
            {
               throw new InvalidDataException("Bloom filter file is too small to contain a header.");
            }

            Header header = _readHeader(stream);
            long expectedBytes = HeaderSize + _byteLength(header.BitCount);
            if (stream.Length < expectedBytes)
            {
               throw new InvalidDataException("Bloom filter file is truncated.");
            }

            MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(
               stream,
               mapName: null,
               capacity: 0,
               MemoryMappedFileAccess.Read,
               HandleInheritability.None,
               leaveOpen: false);
            stream = null!;

            MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor(0, expectedBytes, MemoryMappedFileAccess.Read);
            return new HibpBloomFile(
               mmf,
               accessor,
               path,
               header.Capacity,
               header.BitCount,
               header.HashFunctions,
               header.InsertedCount,
               header.BuiltUtc,
               header.SourceTag,
               writable: false);
         }
         finally
         {
            stream?.Dispose();
         }
      }

      /// <summary>
      /// Creates a new empty writable filter file sized for <paramref name="capacity"/>
      /// and <paramref name="falsePositiveRate"/>.
      /// </summary>
      internal static HibpBloomFile Create(
         string path,
         ulong capacity = BloomSizing.DefaultCapacity,
         double falsePositiveRate = BloomSizing.DefaultFalsePositiveRate,
         string sourceTag = DefaultSourceTag)
      {
         (ulong bitCount, int hashFunctions) = BloomSizing.For(capacity, falsePositiveRate);
         return Create(path, capacity, bitCount, hashFunctions, sourceTag);
      }

      /// <summary>
      /// Creates a new empty writable filter with explicit sizing (tests / custom builds).
      /// </summary>
      internal static HibpBloomFile Create(
         string path,
         ulong capacity,
         ulong bitCount,
         int hashFunctions,
         string sourceTag = DefaultSourceTag)
      {
         ArgumentException.ThrowIfNullOrWhiteSpace(path);
         ArgumentOutOfRangeException.ThrowIfZero(bitCount);
         ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hashFunctions);

         string? directory = System.IO.Path.GetDirectoryName(path);
         if (!string.IsNullOrEmpty(directory))
         {
            _ = Directory.CreateDirectory(directory);
         }

         long fileLength = HeaderSize + _byteLength(bitCount);
         DateTime builtUtc = DateTime.UtcNow;

         using (FileStream sizing = new(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
         {
            sizing.SetLength(fileLength);
            _writeHeader(
               sizing,
               capacity,
               bitCount,
               hashFunctions,
               insertedCount: 0,
               builtUtc,
               sourceTag);
         }

         FileStream? stream = new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
         try
         {
            MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(
               stream,
               mapName: null,
               capacity: 0,
               MemoryMappedFileAccess.ReadWrite,
               HandleInheritability.None,
               leaveOpen: false);
            stream = null;

            MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor(0, fileLength, MemoryMappedFileAccess.ReadWrite);
            return new HibpBloomFile(
               mmf,
               accessor,
               path,
               capacity,
               bitCount,
               hashFunctions,
               insertedCount: 0,
               builtUtc,
               sourceTag,
               writable: true);
         }
         finally
         {
            stream?.Dispose();
         }
      }

      public bool MightContain(ReadOnlySpan<byte> sha1)
      {
         ObjectDisposedException.ThrowIf(_disposed, this);
         _ensureSha1(sha1);

         _positions(sha1, out ulong h1, out ulong h2);
         for (int i = 0; i < _hashFunctions; i++)
         {
            ulong bit = (h1 + ((ulong)i * h2)) % _bitCount;
            if (!_getBit(bit))
            {
               return false;
            }
         }

         return true;
      }

      /// <summary>
      /// Inserts a SHA-1 hash into a writable filter.
      /// </summary>
      internal void Add(ReadOnlySpan<byte> sha1)
      {
         ObjectDisposedException.ThrowIf(_disposed, this);
         if (!_writable)
         {
            throw new InvalidOperationException("Bloom filter was opened read-only.");
         }

         _ensureSha1(sha1);

         _positions(sha1, out ulong h1, out ulong h2);
         for (int i = 0; i < _hashFunctions; i++)
         {
            ulong bit = (h1 + ((ulong)i * h2)) % _bitCount;
            _setBit(bit);
         }

         _insertedCount++;
      }

      /// <summary>
      /// Persists the current inserted count and build timestamp into the header.
      /// </summary>
      internal void CommitHeader()
      {
         ObjectDisposedException.ThrowIf(_disposed, this);
         if (!_writable)
         {
            throw new InvalidOperationException("Bloom filter was opened read-only.");
         }

         BuiltUtc = DateTime.UtcNow;
         byte[] header = new byte[HeaderSize];
         _encodeHeader(
            header,
            Capacity,
            _bitCount,
            _hashFunctions,
            _insertedCount,
            BuiltUtc,
            SourceTag);
         _accessor.WriteArray(0, header, 0, HeaderSize);
         _accessor.Flush();
      }

      public void Dispose()
      {
         if (_disposed)
         {
            return;
         }

         _disposed = true;
         if (_writable)
         {
            try
            {
               CommitHeader();
            }
#pragma warning disable CA1031 // Dispose must not throw when flushing a best-effort header update
            catch
#pragma warning restore CA1031
            {
               // Best effort: the bit array is already on disk.
            }
         }

         _accessor.Dispose();
         _mmf.Dispose();
      }

      private bool _getBit(ulong bitIndex)
      {
         long byteIndex = HeaderSize + (long)(bitIndex >> 3);
         int mask = 1 << (int)(bitIndex & 7);
         byte value = _accessor.ReadByte(byteIndex);
         return (value & mask) != 0;
      }

      private void _setBit(ulong bitIndex)
      {
         long byteIndex = HeaderSize + (long)(bitIndex >> 3);
         int mask = 1 << (int)(bitIndex & 7);
         byte value = _accessor.ReadByte(byteIndex);
         _accessor.Write(byteIndex, (byte)(value | mask));
      }

      private static void _ensureSha1(ReadOnlySpan<byte> sha1)
      {
         if (sha1.Length != Sha1ByteLength)
         {
            throw new ArgumentException($"SHA-1 digest must be {Sha1ByteLength} bytes.", nameof(sha1));
         }
      }

      private static void _positions(ReadOnlySpan<byte> sha1, out ulong h1, out ulong h2)
      {
         h1 = BitConverter.ToUInt64(sha1);
         h2 = BitConverter.ToUInt64(sha1[8..]);
         // Keep the stride odd so (h1 + i*h2) covers the ring for typical m.
         h2 |= 1UL;
      }

      private static long _byteLength(ulong bitCount)
         => (long)((bitCount + 7UL) / 8UL);

      private static Header _readHeader(Stream stream)
      {
         Span<byte> buffer = stackalloc byte[HeaderSize];
         stream.Position = 0;
         stream.ReadExactly(buffer);

         string magic = Encoding.ASCII.GetString(buffer[..4]);
         if (magic != Magic)
         {
            throw new InvalidDataException($"Invalid Bloom filter magic '{magic}'.");
         }

         uint version = BitConverter.ToUInt32(buffer[4..]);
         if (version != FormatVersion)
         {
            throw new InvalidDataException($"Unsupported Bloom filter version {version}.");
         }

         ulong capacity = BitConverter.ToUInt64(buffer[8..]);
         ulong bitCount = BitConverter.ToUInt64(buffer[16..]);
         int hashFunctions = BitConverter.ToInt32(buffer[24..]);
         ulong insertedCount = BitConverter.ToUInt64(buffer[32..]);
         long ticks = BitConverter.ToInt64(buffer[40..]);
         string sourceTag = Encoding.ASCII.GetString(buffer.Slice(48, SourceTagBytes)).TrimEnd('\0');

         if (bitCount == 0 || hashFunctions <= 0)
         {
            throw new InvalidDataException("Bloom filter header has invalid sizing.");
         }

         return new Header(
            capacity,
            bitCount,
            hashFunctions,
            insertedCount,
            new DateTime(ticks, DateTimeKind.Utc),
            sourceTag);
      }

      private static void _writeHeader(
         Stream stream,
         ulong capacity,
         ulong bitCount,
         int hashFunctions,
         ulong insertedCount,
         DateTime builtUtc,
         string sourceTag)
      {
         Span<byte> buffer = stackalloc byte[HeaderSize];
         _encodeHeader(buffer, capacity, bitCount, hashFunctions, insertedCount, builtUtc, sourceTag);
         stream.Position = 0;
         stream.Write(buffer);
         stream.Flush();
      }

      private static void _encodeHeader(
         Span<byte> buffer,
         ulong capacity,
         ulong bitCount,
         int hashFunctions,
         ulong insertedCount,
         DateTime builtUtc,
         string sourceTag)
      {
         buffer.Clear();
         _ = Encoding.ASCII.GetBytes(Magic.AsSpan(), buffer);
         _ = BitConverter.TryWriteBytes(buffer[4..], FormatVersion);
         _ = BitConverter.TryWriteBytes(buffer[8..], capacity);
         _ = BitConverter.TryWriteBytes(buffer[16..], bitCount);
         _ = BitConverter.TryWriteBytes(buffer[24..], hashFunctions);
         _ = BitConverter.TryWriteBytes(buffer[32..], insertedCount);
         _ = BitConverter.TryWriteBytes(buffer[40..], builtUtc.ToUniversalTime().Ticks);

         byte[] tagBytes = Encoding.ASCII.GetBytes(sourceTag ?? DefaultSourceTag);
         int copy = Math.Min(tagBytes.Length, SourceTagBytes);
         tagBytes.AsSpan(0, copy).CopyTo(buffer.Slice(48, SourceTagBytes));
      }

      private readonly record struct Header(
         ulong Capacity,
         ulong BitCount,
         int HashFunctions,
         ulong InsertedCount,
         DateTime BuiltUtc,
         string SourceTag);
   }
}
