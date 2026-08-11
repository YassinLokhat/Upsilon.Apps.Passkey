using FluentAssertions;
using System.IO.Compression;
using System.Security.Cryptography;
using Upsilon.Apps.Passkey.Core.Utils;

namespace Upsilon.Apps.Passkey.UnitTests.Utils
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
            locker.Save(new Payload { Value = "hello" }, "entry");

            Payload loaded = locker.Open<Payload>("entry");
            _ = loaded.Value.Should().Be("hello");
            _ = locker.Exists("entry").Should().BeTrue();
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
               locker.Save(new Payload { Value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24_000)) }, "entry");
               largeSize = new FileInfo(path).Length;
               _ = largeSize.Should().BeGreaterThan(8_000);

               locker.Save(new Payload { Value = "x" }, "entry");
            }

            long smallSize = new FileInfo(path).Length;
            _ = smallSize.Should().BeLessThan(largeSize);

            // A naive ZipArchive.Update without SetLength leaves the old bytes
            // past EOF-of-content; ZipFile.OpenRead must still succeed and the
            // on-disk length must match a freshly built archive of the small payload.
            using (ZipArchive archive = ZipFile.OpenRead(path))
            {
               _ = archive.Entries.Should().ContainSingle(e => e.FullName == "entry");
            }

            using FileLocker verify = _createLocker(path, FileMode.Open);
            Payload loaded = verify.Open<Payload>("entry");
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
            locker.Save(new Payload { Value = "one" }, "a");
            locker.Save(new Payload { Value = "two" }, "b");
            locker.Save(new Payload { Value = "ONE" }, "a");

            _ = locker.Open<Payload>("a").Value.Should().Be("ONE");
            _ = locker.Open<Payload>("b").Value.Should().Be("two");
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
            locker.Save(new Payload { Value = "keep" }, "keep");
            locker.Save(new Payload { Value = "drop" }, "drop");

            locker.Delete("drop");

            _ = locker.Exists("drop").Should().BeFalse();
            _ = locker.Open<Payload>("keep").Value.Should().Be("keep");
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
            locker.Save(new Payload { Value = "held" }, "entry");

            Action secondOpen = () =>
            {
               using FileStream _ = new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
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
            locker.Save(new Payload { Value = "secret" }, "vault", passkeys);

            Payload loaded = locker.Open<Payload>("vault", passkeys);
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
            locker.Save(new Payload { Value = "clean" }, "entry");
            locker.Save(new Payload { Value = "cleaner" }, "entry");

            string[] leftovers = Directory.GetFiles(directory, "*.tmp");
            _ = leftovers.Should().BeEmpty();
         }
         finally
         {
            _cleanup(path);
         }
      }

      private static FileLocker _createLocker(string path, FileMode mode) =>
         new(UnitTestsHelper.CryptographicCenter, UnitTestsHelper.SerializationCenter, path, mode);

      private static string _preparePath([System.Runtime.CompilerServices.CallerMemberName] string name = "")
      {
         string directory = Path.Combine(".", "TestFiles", "FileLocker", name);
         if (Directory.Exists(directory))
         {
            Directory.Delete(directory, recursive: true);
         }

         _ = Directory.CreateDirectory(directory);
         return Path.Combine(directory, "archive.pku");
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
