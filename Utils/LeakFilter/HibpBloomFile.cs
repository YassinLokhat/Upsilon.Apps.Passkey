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

      // Little-endian field offsets inside the HeaderSize-byte header.
      private const int MAGIC_OFFSET = 0;
      private const int VERSION_OFFSET = 4;
      private const int CAPACITY_OFFSET = 8;
      private const int BIT_COUNT_OFFSET = 16;
      private const int HASH_FUNCTIONS_OFFSET = 24;
      private const int INSERTED_COUNT_OFFSET = 32;
      private const int BUILT_UTC_TICKS_OFFSET = 40;
      private const int SOURCE_TAG_OFFSET = 48;

      private readonly FileStream _file;
      private readonly MemoryMappedFile _mmf;
      private readonly MemoryMappedViewAccessor _accessor;
      private readonly bool _writable;
      private bool _disposed;

      /// <summary>
      /// Maps an existing <c>.pkbf</c>. The <see cref="FileStream"/> is owned by this
      /// instance (<c>leaveOpen: true</c>) and released by <see cref="Dispose"/>: the
      /// mapping outlives this constructor, so it must not be scoped to a <c>using</c>.
      /// </summary>
      private HibpBloomFile(string path, bool writable)
      {
         FileAccess fileAccess = writable ? FileAccess.ReadWrite : FileAccess.Read;
         MemoryMappedFileAccess mmfAccess = writable
            ? MemoryMappedFileAccess.ReadWrite
            : MemoryMappedFileAccess.Read;

         _file = new FileStream(path, FileMode.Open, fileAccess, FileShare.Read);
         MemoryMappedFile? mmf = null;
         MemoryMappedViewAccessor? accessor = null;
         try
         {
            if (_file.Length < HeaderSize)
            {
               throw new InvalidDataException("Bloom filter file is too small to contain a header.");
            }

            Header header = _readHeader(_file);
            long expectedBytes = HeaderSize + _byteLength(header.BitCount);
            if (_file.Length < expectedBytes)
            {
               throw new InvalidDataException("Bloom filter file is truncated.");
            }

            mmf = MemoryMappedFile.CreateFromFile(
               _file,
               mapName: null,
               capacity: 0,
               mmfAccess,
               HandleInheritability.None,
               leaveOpen: true);
            accessor = mmf.CreateViewAccessor(0, expectedBytes, mmfAccess);

            _mmf = mmf;
            _accessor = accessor;
            Path = path;
            Capacity = header.Capacity;
            BitCount = header.BitCount;
            HashFunctions = header.HashFunctions;
            InsertedCount = header.InsertedCount;
            BuiltUtc = header.BuiltUtc;
            SourceTag = header.SourceTag;
            _writable = writable;
         }
         catch
         {
            accessor?.Dispose();
            mmf?.Dispose();
            _file.Dispose();
            throw;
         }
      }

      public string? Path { get; }

      public DateTime BuiltUtc { get; private set; }

      public ulong InsertedCount { get; private set; }

      internal ulong Capacity { get; }

      internal ulong BitCount { get; }

      internal int HashFunctions { get; }

      internal string SourceTag { get; }

      /// <summary>
      /// Opens an existing <c>.pkbf</c> for read-only membership queries.
      /// </summary>
      internal static HibpBloomFile Open(string path)
      {
         ArgumentException.ThrowIfNullOrWhiteSpace(path);
         return new HibpBloomFile(path, writable: false);
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

         return new HibpBloomFile(path, writable: true);
      }

      public bool MightContain(ReadOnlySpan<byte> sha1)
      {
         ObjectDisposedException.ThrowIf(_disposed, this);
         _ensureSha1(sha1);

         _positions(sha1, out ulong h1, out ulong h2);
         for (int i = 0; i < HashFunctions; i++)
         {
            ulong bit = (h1 + ((ulong)i * h2)) % BitCount;
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
         for (int i = 0; i < HashFunctions; i++)
         {
            ulong bit = (h1 + ((ulong)i * h2)) % BitCount;
            _setBit(bit);
         }

         InsertedCount++;
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
            BitCount,
            HashFunctions,
            InsertedCount,
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

         // Commit before the disposed guard closes: the bit array is already on
         // disk, only the inserted count and timestamp are at stake here.
         if (_writable)
         {
            try
            {
               CommitHeader();
            }
            catch (Exception ex)
               when (ex is ObjectDisposedException
               or ArgumentException
               or NotSupportedException)
            {
               System.Diagnostics.Trace.TraceWarning($"Best effort: the bit array is already on disk: {ex}");
            }
         }

         _disposed = true;
         _accessor.Dispose();
         _mmf.Dispose();
         _file.Dispose();
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

         string magic = Encoding.ASCII.GetString(buffer.Slice(MAGIC_OFFSET, Magic.Length));
         if (magic != Magic)
         {
            throw new InvalidDataException($"Invalid Bloom filter magic '{magic}'.");
         }

         uint version = BitConverter.ToUInt32(buffer[VERSION_OFFSET..]);
         if (version != FormatVersion)
         {
            throw new InvalidDataException($"Unsupported Bloom filter version {version}.");
         }

         ulong capacity = BitConverter.ToUInt64(buffer[CAPACITY_OFFSET..]);
         ulong bitCount = BitConverter.ToUInt64(buffer[BIT_COUNT_OFFSET..]);
         int hashFunctions = BitConverter.ToInt32(buffer[HASH_FUNCTIONS_OFFSET..]);
         ulong insertedCount = BitConverter.ToUInt64(buffer[INSERTED_COUNT_OFFSET..]);
         long ticks = BitConverter.ToInt64(buffer[BUILT_UTC_TICKS_OFFSET..]);
         string sourceTag = Encoding.ASCII.GetString(buffer.Slice(SOURCE_TAG_OFFSET, SourceTagBytes)).TrimEnd('\0');

         return bitCount == 0 || hashFunctions <= 0
            ? throw new InvalidDataException("Bloom filter header has invalid sizing.")
            : new Header(
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
         _ = Encoding.ASCII.GetBytes(Magic.AsSpan(), buffer[MAGIC_OFFSET..]);
         _ = BitConverter.TryWriteBytes(buffer[VERSION_OFFSET..], FormatVersion);
         _ = BitConverter.TryWriteBytes(buffer[CAPACITY_OFFSET..], capacity);
         _ = BitConverter.TryWriteBytes(buffer[BIT_COUNT_OFFSET..], bitCount);
         _ = BitConverter.TryWriteBytes(buffer[HASH_FUNCTIONS_OFFSET..], hashFunctions);
         _ = BitConverter.TryWriteBytes(buffer[INSERTED_COUNT_OFFSET..], insertedCount);
         _ = BitConverter.TryWriteBytes(buffer[BUILT_UTC_TICKS_OFFSET..], builtUtc.ToUniversalTime().Ticks);

         byte[] tagBytes = Encoding.ASCII.GetBytes(sourceTag ?? DefaultSourceTag);
         int copy = Math.Min(tagBytes.Length, SourceTagBytes);
         tagBytes.AsSpan(0, copy).CopyTo(buffer.Slice(SOURCE_TAG_OFFSET, SourceTagBytes));
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
