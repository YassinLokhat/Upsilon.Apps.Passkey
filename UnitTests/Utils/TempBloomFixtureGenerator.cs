using FluentAssertions;
using System.Text.Json;
using Upsilon.Apps.Passkey.Utils.LeakFilter;

namespace Upsilon.Apps.Passkey.UnitTests.Utils
{
   /// <summary>
   /// TEMPORARY smoke-test helper: writes a tiny <c>.pkbf</c> at the path stored
   /// in the WPF host's <c>config.json</c> so the offline Bloom fallback can be
   /// exercised without downloading the full HIBP corpus. Delete once done.
   /// </summary>
   [TestClass]
   public sealed class TempBloomFixtureGenerator
   {
      private static readonly string[] _seedPasswords =
      [
         "test",
         "password",
         "123456",
         "qwerty",
         "admin",
         "letmein",
         "iloveyou",
         "welcome",
      ];

      [TestMethod]
      public void GenerateBloomFixtureAtConfiguredPath()
      {
         string configFile = _findGuiConfigFile();
         Console.WriteLine($"config.json : {configFile}");

         string filterPath = _readFilterPath(configFile);
         Console.WriteLine($"FilterPath  : {filterPath}");

         const ulong capacity = 1_000;
         (ulong bits, int hashFunctions) = BloomSizing.For(capacity, 0.01);

         using (HibpBloomFile writable = HibpBloomFile.Create(filterPath, capacity, bits, hashFunctions))
         {
            foreach (string password in _seedPasswords)
            {
               writable.Add(BloomTestHelper.Sha1(password));
            }

            writable.CommitHeader();
         }

         Console.WriteLine($"ecrit       : {new FileInfo(filterPath).Length} octets");

         // Re-open through the exact production entry point the WPF host uses.
         LeakFilterConfig config = new() { Enabled = true, FilterPath = filterPath };
         using ILocalLeakFilter? filter = config.TryOpenConfiguredFilter();

         _ = filter.Should().NotBeNull();
         _ = filter!.InsertedCount.Should().Be((ulong)_seedPasswords.Length);

         foreach (string password in _seedPasswords)
         {
            Console.WriteLine($"MightContain(\"{password}\") : {filter.MightContain(BloomTestHelper.Sha1(password))}");
            _ = filter.MightContain(BloomTestHelper.Sha1(password)).Should().BeTrue();
         }

         _ = filter.MightContain(BloomTestHelper.Sha1("a-password-not-seeded-in-this-fixture")).Should().BeFalse();
      }

      private static string _findGuiConfigFile()
      {
         DirectoryInfo? directory = new(AppContext.BaseDirectory);

         while (directory is not null
            && !Directory.Exists(Path.Combine(directory.FullName, "GUI", "WPF")))
         {
            directory = directory.Parent;
         }

         _ = directory.Should().NotBeNull("the repository root must be reachable from the test output folder");

         string guiDebug = Path.Combine(directory!.FullName, "GUI", "WPF", "bin", "Debug");
         _ = Directory.Exists(guiDebug).Should().BeTrue("the WPF host must have been built at least once");

         string[] candidates = Directory.GetFiles(guiDebug, "config.json", SearchOption.AllDirectories);
         _ = candidates.Should().NotBeEmpty("the WPF host must have run at least once to write config.json");

         return candidates[0];
      }

      private static string _readFilterPath(string configFile)
      {
         using JsonDocument document = JsonDocument.Parse(File.ReadAllText(configFile));

         string? filterPath = document.RootElement
            .GetProperty("LeakFilterConfig")
            .GetProperty("FilterPath")
            .GetString();

         _ = filterPath.Should().NotBeNullOrWhiteSpace();

         return filterPath!;
      }
   }
}
