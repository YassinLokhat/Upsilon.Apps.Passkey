using System.Text;

namespace Upsilon.Apps.Passkey.Utils.LeakFilter
{
   /// <summary>
   /// Sidecar (<c>.ranges</c>) recording, for every HIBP hash-range prefix, the
   /// <c>ETag</c> of the payload already folded into a <c>.pkbf</c>.
   /// <para>
   /// It buys two things. A refresh can revalidate each range with
   /// <c>If-None-Match</c> and download only what actually changed, and an
   /// interrupted full build can resume instead of starting the corpus over.
   /// </para>
   /// <para>
   /// The recorded <see cref="HibpBloomStamp"/> ties the sidecar to one committed
   /// state of one filter file. A filter that was deleted, rebuilt or replaced no
   /// longer matches, and the sidecar is then rejected rather than trusted — the
   /// alternative would be skipping ranges whose bits are absent, i.e. silent
   /// false negatives on a leak check.
   /// </para>
   /// </summary>
   internal sealed class HibpRangeStateStore : IDisposable
   {
      internal const string Magic = "PKRS";
      internal const uint FormatVersion = 1;
      internal const int HeaderSize = 64;
      internal const int EntryStride = 32;
      internal const string FileSuffix = ".ranges";

      /// <summary>
      /// Inline room for an ETag, in ASCII bytes. HIBP serves Azure-style tags
      /// (<c>"0x8DED132F654ED41"</c>, 19 bytes); a longer one simply is not
      /// cached, which costs one unconditional download.
      /// </summary>
      internal const int MaxEtagLength = EntryStride - ETAG_OFFSET;

      // Little-endian field offsets inside the HeaderSize-byte header. Bytes 0..3
      // carry Magic, which a format has to expose first to be identifiable at all.
      private const int VERSION_OFFSET = 4;
      private const int ENTRY_STRIDE_OFFSET = 8;
      private const int PREFIX_COUNT_OFFSET = 12;
      private const int CAPACITY_OFFSET = 16;
      private const int BIT_COUNT_OFFSET = 24;
      private const int HASH_FUNCTIONS_OFFSET = 32;
      private const int INSERTED_COUNT_OFFSET = 40;
      private const int BUILT_UTC_TICKS_OFFSET = 48;

      // Field offsets inside an EntryStride-byte range record.
      private const int STATE_OFFSET = 0;
      private const int ETAG_LENGTH_OFFSET = 1;
      private const int ETAG_OFFSET = 2;

      private const byte STATE_INGESTED = 1;

      private readonly System.Threading.Lock _gate = new();
      private readonly FileStream _file;
      private readonly byte[] _entries;
      private readonly List<int> _pending = [];
      private readonly int _prefixCount;
      private int _ingestedPrefixes;
      private bool _disposed;

      private HibpRangeStateStore(FileStream file, byte[] entries, int prefixCount, int ingestedPrefixes)
      {
         _file = file;
         _entries = entries;
         _prefixCount = prefixCount;
         _ingestedPrefixes = ingestedPrefixes;
      }

      /// <summary>
      /// Number of prefixes already folded into the filter.
      /// </summary>
      internal int IngestedPrefixes
      {
         get
         {
            lock (_gate)
            {
               return _ingestedPrefixes;
            }
         }
      }

      /// <summary>
      /// Entries marked since the last <see cref="Commit"/>.
      /// </summary>
      internal int PendingCount
      {
         get
         {
            lock (_gate)
            {
               return _pending.Count;
            }
         }
      }

      /// <summary>
      /// Resolves the sidecar path for a <c>.pkbf</c>.
      /// </summary>
      internal static string PathFor(string filterPath)
      {
         ArgumentException.ThrowIfNullOrWhiteSpace(filterPath);
         return filterPath + FileSuffix;
      }

      /// <summary>
      /// Creates an empty sidecar for <paramref name="filter"/>, replacing any
      /// existing file.
      /// </summary>
      internal static HibpRangeStateStore CreateNew(string path, int prefixCount, HibpBloomFile filter)
      {
         ArgumentException.ThrowIfNullOrWhiteSpace(path);
         ArgumentOutOfRangeException.ThrowIfNegativeOrZero(prefixCount);
         ArgumentNullException.ThrowIfNull(filter);

         string? directory = System.IO.Path.GetDirectoryName(path);
         if (!string.IsNullOrEmpty(directory))
         {
            _ = Directory.CreateDirectory(directory);
         }

         FileStream file = new(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
         try
         {
            file.SetLength(HeaderSize + ((long)prefixCount * EntryStride));
            HibpRangeStateStore store = new(file, new byte[(long)prefixCount * EntryStride], prefixCount, ingestedPrefixes: 0);
            store._writeHeader(filter);
            file.Flush(flushToDisk: true);
            return store;
         }
         catch
         {
            file.Dispose();
            throw;
         }
      }

      /// <summary>
      /// Opens the sidecar when it exists and still describes the committed state
      /// of <paramref name="filter"/>; returns <see langword="null"/> otherwise, in
      /// which case every range has to be re-fetched.
      /// </summary>
      internal static HibpRangeStateStore? TryOpen(string path, int prefixCount, HibpBloomFile filter)
      {
         ArgumentException.ThrowIfNullOrWhiteSpace(path);
         ArgumentOutOfRangeException.ThrowIfNegativeOrZero(prefixCount);
         ArgumentNullException.ThrowIfNull(filter);

         if (!File.Exists(path))
         {
            return null;
         }

         FileStream? file = null;
         try
         {
            file = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            long expectedLength = HeaderSize + ((long)prefixCount * EntryStride);
            if (file.Length < expectedLength || !_headerMatches(file, prefixCount, filter))
            {
               file.Dispose();
               return null;
            }

            byte[] entries = new byte[(long)prefixCount * EntryStride];
            file.Position = HeaderSize;
            file.ReadExactly(entries);

            int ingested = 0;
            for (int prefix = 0; prefix < prefixCount; prefix++)
            {
               if (entries[(prefix * EntryStride) + STATE_OFFSET] == STATE_INGESTED)
               {
                  ingested++;
               }
            }

            return new HibpRangeStateStore(file, entries, prefixCount, ingested);
         }
         catch (Exception ex)
            when (ex is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or ArgumentException
            or System.Security.SecurityException)
         {
            file?.Dispose();
            System.Diagnostics.Trace.TraceWarning($"HIBP range sidecar could not be opened, every range will be re-fetched: {ex}");
            return null;
         }
      }

      /// <summary>
      /// Deletes the sidecar of <paramref name="filterPath"/> when present.
      /// </summary>
      internal static void DeleteFor(string filterPath)
      {
         string path = PathFor(filterPath);
         if (File.Exists(path))
         {
            File.Delete(path);
         }
      }

      /// <summary>
      /// Tells whether <paramref name="prefix"/> is already folded into the filter,
      /// and yields the ETag it was fetched with when the server sent one.
      /// </summary>
      internal bool TryGetIngested(int prefix, out string? etag)
      {
         ObjectDisposedException.ThrowIf(_disposed, this);
         _ensurePrefix(prefix);

         etag = null;
         int offset = prefix * EntryStride;
         if (_entries[offset + STATE_OFFSET] != STATE_INGESTED)
         {
            return false;
         }

         int length = _entries[offset + ETAG_LENGTH_OFFSET];
         if (length is > 0 and <= MaxEtagLength)
         {
            etag = Encoding.ASCII.GetString(_entries, offset + ETAG_OFFSET, length);
         }

         return true;
      }

      /// <summary>
      /// Buffers <paramref name="prefix"/> as folded in. Only persisted by
      /// <see cref="Commit"/>, which flushes the filter first.
      /// </summary>
      internal void MarkIngested(int prefix, string? etag)
      {
         ObjectDisposedException.ThrowIf(_disposed, this);
         _ensurePrefix(prefix);

         int offset = prefix * EntryStride;
         int length = etag is not null && etag.Length <= MaxEtagLength && Ascii.IsValid(etag)
            ? etag.Length
            : 0;

         lock (_gate)
         {
            if (_entries[offset + STATE_OFFSET] != STATE_INGESTED)
            {
               _ingestedPrefixes++;
            }

            Array.Clear(_entries, offset, EntryStride);
            _entries[offset + STATE_OFFSET] = STATE_INGESTED;
            _entries[offset + ETAG_LENGTH_OFFSET] = (byte)length;
            if (length > 0)
            {
               _ = Ascii.FromUtf16(etag!, _entries.AsSpan(offset + ETAG_OFFSET, length), out _);
            }

            _pending.Add(prefix);
         }
      }

      /// <summary>
      /// Checkpoints the pair: buffered entries are snapshotted, then the filter is
      /// flushed and its header committed, then the entries are persisted against
      /// that fresh stamp.
      /// <para>
      /// The snapshot has to come first. The flush that follows necessarily covers
      /// every bit set before those prefixes were marked, so an interruption can
      /// only leave the sidecar behind the filter — never ahead of it.
      /// </para>
      /// </summary>
      internal void Commit(HibpBloomFile filter)
      {
         ObjectDisposedException.ThrowIf(_disposed, this);
         ArgumentNullException.ThrowIfNull(filter);

         int[] batch;
         lock (_gate)
         {
            batch = [.. _pending];
            _pending.Clear();
         }

         filter.Flush();
         filter.CommitHeader();

         Array.Sort(batch);
         foreach (int prefix in batch)
         {
            _file.Position = HeaderSize + ((long)prefix * EntryStride);
            _file.Write(_entries.AsSpan(prefix * EntryStride, EntryStride));
         }

         _writeHeader(filter);
         _file.Flush(flushToDisk: true);
      }

      public void Dispose()
      {
         if (_disposed)
         {
            return;
         }

         _disposed = true;
         _file.Dispose();
      }

      private void _ensurePrefix(int prefix)
         => ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)prefix, (uint)_prefixCount, nameof(prefix));

      private void _writeHeader(HibpBloomFile filter)
      {
         HibpBloomStamp stamp = filter.LastStamp;
         Span<byte> buffer = stackalloc byte[HeaderSize];
         buffer.Clear();
         _ = Encoding.ASCII.GetBytes(Magic.AsSpan(), buffer);
         _ = BitConverter.TryWriteBytes(buffer[VERSION_OFFSET..], FormatVersion);
         _ = BitConverter.TryWriteBytes(buffer[ENTRY_STRIDE_OFFSET..], EntryStride);
         _ = BitConverter.TryWriteBytes(buffer[PREFIX_COUNT_OFFSET..], _prefixCount);
         _ = BitConverter.TryWriteBytes(buffer[CAPACITY_OFFSET..], filter.Capacity);
         _ = BitConverter.TryWriteBytes(buffer[BIT_COUNT_OFFSET..], filter.BitCount);
         _ = BitConverter.TryWriteBytes(buffer[HASH_FUNCTIONS_OFFSET..], filter.HashFunctions);
         _ = BitConverter.TryWriteBytes(buffer[INSERTED_COUNT_OFFSET..], stamp.InsertedCount);
         _ = BitConverter.TryWriteBytes(buffer[BUILT_UTC_TICKS_OFFSET..], stamp.BuiltUtcTicks);

         _file.Position = 0;
         _file.Write(buffer);
      }

      private static bool _headerMatches(FileStream file, int prefixCount, HibpBloomFile filter)
      {
         Span<byte> buffer = stackalloc byte[HeaderSize];
         file.Position = 0;
         file.ReadExactly(buffer);

         if (Encoding.ASCII.GetString(buffer[..Magic.Length]) != Magic
            || BitConverter.ToUInt32(buffer[VERSION_OFFSET..]) != FormatVersion
            || BitConverter.ToInt32(buffer[ENTRY_STRIDE_OFFSET..]) != EntryStride
            || BitConverter.ToInt32(buffer[PREFIX_COUNT_OFFSET..]) != prefixCount)
         {
            return false;
         }

         if (BitConverter.ToUInt64(buffer[CAPACITY_OFFSET..]) != filter.Capacity
            || BitConverter.ToUInt64(buffer[BIT_COUNT_OFFSET..]) != filter.BitCount
            || BitConverter.ToInt32(buffer[HASH_FUNCTIONS_OFFSET..]) != filter.HashFunctions)
         {
            return false;
         }

         HibpBloomStamp stamp = filter.LastStamp;
         return BitConverter.ToUInt64(buffer[INSERTED_COUNT_OFFSET..]) == stamp.InsertedCount
            && BitConverter.ToInt64(buffer[BUILT_UTC_TICKS_OFFSET..]) == stamp.BuiltUtcTicks;
      }
   }
}
