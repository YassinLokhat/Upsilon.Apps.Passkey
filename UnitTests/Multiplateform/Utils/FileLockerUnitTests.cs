using FluentAssertions;
using System.IO.Compression;
using System.Security.Cryptography;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.UnitTests.Multiplateform.Utils
{
   [TestClass]
   public sealed class FileLockerUnitTests
   {
      [TestMethod]
      public void Case01_SaveOpenRoundTrip_PreservesPayload()
      {
         string path = _preparePath();

         try
         {
            using FileLocker locker = _createLocker(path, FileMode.CreateNew);
            locker.Save(new Payload { Value = "hello" }, "header");

            Payload loaded = locker.Open<Payload>("header");
            _ = loaded.Value.Should().Be("hello");
            _ = locker.Exists("header").Should().BeTrue();
         }
         finally
         {
            _cleanup(path);
         }
      }

      [TestMethod]
      public void Case02_ReplacingLargeEntryWithSmall_TruncatesFileWithoutTrailingGarbage()
      {
         string path = _preparePath();

         try
         {
            long largeSize;
            using (FileLocker locker = _createLocker(path, FileMode.CreateNew))
            {
               // Random bytes compress poorly, so the on-disk archive actually grows.
               locker.Save(new Payload { Value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24_000)) }, "header");
               largeSize = new FileInfo(path).Length;
               _ = largeSize.Should().BeGreaterThan(8_000);

               locker.Save(new Payload { Value = "x" }, "header");
            }

            long smallSize = new FileInfo(path).Length;
            _ = smallSize.Should().BeLessThan(largeSize);

            // A naive ZipArchive.Update without SetLength leaves the old bytes
            // past EOF-of-content; ZipFile.OpenRead must still succeed and the
            // on-disk length must match a freshly built archive of the small payload.
            using (ZipArchive archive = ZipFile.OpenRead(path))
            {
               _ = archive.Entries.Should().ContainSingle(e => e.FullName == "header");
            }

            using FileLocker verify = _createLocker(path, FileMode.Open);
            Payload loaded = verify.Open<Payload>("header");
            _ = loaded.Value.Should().Be("x");
         }
         finally
         {
            _cleanup(path);
         }
      }

      [TestMethod]
      public void Case03_UpdatingOneEntry_PreservesSiblings()
      {
         string path = _preparePath();

         try
         {
            using FileLocker locker = _createLocker(path, FileMode.CreateNew);
            locker.Save(new Payload { Value = "one" }, "header");
            locker.Save(new Payload { Value = "two" }, "database");
            locker.Save(new Payload { Value = "ONE" }, "header");

            _ = locker.Open<Payload>("header").Value.Should().Be("ONE");
            _ = locker.Open<Payload>("database").Value.Should().Be("two");
         }
         finally
         {
            _cleanup(path);
         }
      }

      [TestMethod]
      public void Case04_DeleteEntry_RemovesOnlyThatEntry()
      {
         string path = _preparePath();

         try
         {
            using FileLocker locker = _createLocker(path, FileMode.CreateNew);
            locker.Save(new Payload { Value = "header" }, "header");
            locker.Save(new Payload { Value = "autosave" }, "autosave");

            locker.Delete("autosave");

            _ = locker.Exists("autosave").Should().BeFalse();
            _ = locker.Open<Payload>("header").Value.Should().Be("header");
         }
         finally
         {
            _cleanup(path);
         }
      }

      [TestMethod]
      public void Case05_AfterAtomicSave_HandleIsHeldAgain()
      {
         string path = _preparePath();

         try
         {
            using FileLocker locker = _createLocker(path, FileMode.CreateNew);
            locker.Save(new Payload { Value = "held" }, "header");

            // Product contract: a second session cannot open the same vault for
            // writing (sibling .pku.lock with FileShare.None — required on Linux
            // where .pku FileShare.Read|Delete alone does not block writers).
            Action secondOpen = () =>
            {
               using FileLocker _ = _createLocker(path, FileMode.Open);
            };

            _ = secondOpen.Should().Throw<IOException>();
         }
         finally
         {
            _cleanup(path);
         }
      }

      [TestMethod]
      public void Case06_EncryptedEntry_RoundTrip()
      {
         string path = _preparePath();
         string[] passkeys = ["p1", "p2"];

         try
         {
            using FileLocker locker = _createLocker(path, FileMode.CreateNew);
            locker.Save(new Payload { Value = "secret" }, "database", passkeys);

            Payload loaded = locker.Open<Payload>("database", passkeys);
            _ = loaded.Value.Should().Be("secret");
         }
         finally
         {
            _cleanup(path);
         }
      }

      [TestMethod]
      public void Case07_NoTempFilesLeftAfterSave()
      {
         string path = _preparePath();
         string directory = Path.GetDirectoryName(path)!;

         try
         {
            using FileLocker locker = _createLocker(path, FileMode.CreateNew);
            locker.Save(new Payload { Value = "clean" }, "header");
            locker.Save(new Payload { Value = "cleaner" }, "header");

            string[] leftovers = Directory.GetFiles(directory, "*.tmp");
            _ = leftovers.Should().BeEmpty();
         }
         finally
         {
            _cleanup(path);
         }
      }

      [TestMethod]
      public void Case08_RapidConsecutiveSaves_DoNotFailOnReplace()
      {
         string path = _preparePath();

         try
         {
            using FileLocker locker = _createLocker(path, FileMode.CreateNew);

            for (int i = 0; i < 40; i++)
            {
               locker.Save(new Payload { Value = $"v{i}" }, "header");
               locker.Save(new Payload { Value = $"act-{i}" }, "activity");
            }

            _ = locker.Open<Payload>("header").Value.Should().Be("v39");
            _ = locker.Open<Payload>("activity").Value.Should().Be("act-39");
            _ = Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp").Should().BeEmpty();
         }
         finally
         {
            _cleanup(path);
         }
      }

      [TestMethod]
      public void Case09_DisallowedEntryName_IsRejectedOnSave()
      {
         string path = _preparePath();

         try
         {
            using FileLocker locker = _createLocker(path, FileMode.CreateNew);
            Action save = () => locker.Save(new Payload { Value = "x" }, "evil");
            _ = save.Should().Throw<ArgumentException>();
         }
         finally
         {
            _cleanup(path);
         }
      }

      [TestMethod]
      public void Case10_GzipBombEntry_ThrowsCorruptedSource()
      {
         string path = _preparePath();

         try
         {
            // Highly compressible payload: small on disk, huge when inflated past the ceiling.
            byte[] zeros = new byte[ResourceBudgets.MaxEntryDecodedBytes + 1];
            string bomb;
            using (MemoryStream raw = new(zeros))
            using (MemoryStream compressed = new())
            {
               using (GZipStream gzip = new(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
               {
                  raw.CopyTo(gzip);
               }

               bomb = Convert.ToBase64String(compressed.ToArray());
            }

            using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
               ZipArchiveEntry entry = archive.CreateEntry("header");
               using StreamWriter writer = new(entry.Open());
               writer.Write(bomb);
            }

            using FileLocker locker = _createLocker(path, FileMode.Open);
            Action open = () => locker.Open<Payload>("header");
            _ = open.Should().Throw<CorruptedSourceException>();
         }
         finally
         {
            _cleanup(path);
         }
      }

      [TestMethod]
      public void Case11_UnexpectedZipEntry_ThrowsCorruptedSource()
      {
         string path = _preparePath();

         try
         {
            using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
               ZipArchiveEntry entry = archive.CreateEntry("not-a-vault-entry");
               using StreamWriter writer = new(entry.Open());
               writer.Write("x");
            }

            using FileLocker locker = _createLocker(path, FileMode.Open);
            Action open = () => locker.Open<Payload>("header");
            _ = open.Should().Throw<CorruptedSourceException>();
         }
         finally
         {
            _cleanup(path);
         }
      }

      private static FileLocker _createLocker(string path, FileMode mode) =>
         new(UnitTestsHelper.CryptographyCenter, UnitTestsHelper.SerializationCenter, path, mode);

      private static string _preparePath([System.Runtime.CompilerServices.CallerMemberName] string name = "")
      {
         string directory = Path.Join(".", "TestFiles", "FileLocker", name);
         if (Directory.Exists(directory))
         {
            Directory.Delete(directory, recursive: true);
         }

         _ = Directory.CreateDirectory(directory);
         return Path.Join(directory, "archive.pku");
      }

      private static void _cleanup(string path)
      {
         string? directory = Path.GetDirectoryName(path);
         if (!string.IsNullOrEmpty(directory)
            && Directory.Exists(directory))
         {
            try
            {
               Directory.Delete(directory, recursive: true);
            }
            catch
            {
               // Best-effort: Windows may briefly keep a handle after dispose.
            }
         }
      }

      private sealed class Payload
      {
         public string Value { get; set; } = string.Empty;
      }
   }
}
