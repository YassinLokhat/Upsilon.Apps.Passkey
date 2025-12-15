using FluentAssertions;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.UnitTests
{
   internal static class UnitTestsHelper
   {
      public static readonly int RANDOMIZED_TESTS_LOOP = 10;

      public static readonly ICryptographyCenter CryptographicCenter = new CryptographyCenter();
      public static readonly ISerializationCenter SerializationCenter = new JsonSerializationCenter();
      public static readonly IPasswordFactory PasswordFactory = new PasswordFactory();
      public static readonly IClipboardManager ClipboardManager = new ClipboardManager();

      public static string ComputeTestDirectory([CallerMemberName] string username = "") => $"./TestFiles/{username}";
      public static string ComputeDatabaseFileDirectory([CallerMemberName] string username = "") => $"{ComputeTestDirectory(username)}/{CryptographicCenter.GetHash(username)}";
      public static string ComputeDatabaseFilePath([CallerMemberName] string username = "") => $"{ComputeDatabaseFileDirectory(username)}/{CryptographicCenter.GetHash(username)}.pku";

      public static string ReadFileZipEntry(string zipFile, string fileEntry)
      {
         using ZipArchive archive = ZipFile.OpenRead(zipFile);
         ZipArchiveEntry zipEntry = archive.GetEntry(fileEntry)
            ?? throw new FileNotFoundException($"The file entry '{fileEntry}' not found in the archive {zipFile}.", $"{zipFile}/{fileEntry}");

         using Stream stream = zipEntry.Open();
         using StreamReader reader = new(stream, Encoding.UTF8);

         return reader.ReadToEnd();
      }

      public static string GetTestFilePath(string fileName, bool createIfNotExists = false)
      {
         string filePath = $"./TestFiles/{fileName}";

         if (!File.Exists(filePath)
            && createIfNotExists)
         {
            string fileDirectory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(fileDirectory))
            {
               Directory.CreateDirectory(fileDirectory);
            }

            File.Create(filePath).Close();
         }

         return filePath;
      }

      public static IDatabase CreateTestDatabase(string[] passkeys = null, [CallerMemberName] string username = "")
      {
         string databaseFile = ComputeDatabaseFilePath(username);

         passkeys ??= GetRandomStringArray();

         IDatabase database = Database.Create(CryptographicCenter,
            SerializationCenter,
            PasswordFactory,
            ClipboardManager,
            databaseFile,
            username,
            passkeys);

         return database;
      }

      public static IDatabase OpenTestDatabase(string[] passkeys, out IWarning[] detectedWarnings, AutoSaveMergeBehavior mergeAutoSave = AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile, [CallerMemberName] string username = "")
      {
         string databaseFile = ComputeDatabaseFilePath(username);

         IWarning[] warnings = [];

         IDatabase database = Database.Open(CryptographicCenter,
            SerializationCenter,
            PasswordFactory,
            ClipboardManager,
            databaseFile,
            username);

         database.AutoSaveDetected += (s, e) => { e.MergeBehavior = mergeAutoSave; };
         database.WarningDetected += (s, e) => { warnings = e.Warnings; };

         foreach (string passkey in passkeys)
         {
            _ = database.Login(passkey);
         }

         detectedWarnings = warnings;

         return database;
      }

      public static void ClearTestEnvironment([CallerMemberName] string username = "")
      {
         string directory = ComputeTestDirectory(username);

         if (Directory.Exists(directory))
         {
            Directory.Delete(directory, true);
         }
      }

      public static string GetUsername([CallerMemberName] string username = "") => username;

      private static RandomNumberGenerator _random => RandomNumberGenerator.Create();

      public static string[] GetRandomStringArray(int count = 0)
      {
         if (count == 0)
         {
            count = GetRandomInt(2, 5);
         }

         List<string> passkeys = [];
         for (int i = 0; i < count; i++)
         {
            passkeys.Add(GetRandomString());
         }

         return [.. passkeys];
      }

      public static string GetRandomString(int min = 10, int max = 0)
      {
         if (max == 0)
         {
            max = min + 10;
         }

         int length = GetRandomInt(min, max);

         byte[] randomBytes = new byte[length];
         _random.GetBytes(randomBytes);

         return Convert.ToBase64String(randomBytes)[..length];
      }

      public static int GetRandomInt(int max) => GetRandomInt(0, max);

      public static int GetRandomInt(int min, int max)
      {
         byte[] randomBytes = new byte[4];
         _random.GetBytes(randomBytes);

         uint value = BitConverter.ToUInt32(randomBytes, 0);

         uint interval = (uint)(max - min);
         value = value % interval;
         value += (uint)min;

         return (int)value;
      }

      public static void LastLogsShouldMatch(IDatabase database, string[] expectedLogs)
      {
         string[] actualLogs = database.Logs.Select(x => $"{(x.NeedsReview ? "Warning" : "Information")} : {x.Message}").ToArray();

         _lastLogsShouldMatch(actualLogs, expectedLogs);
      }

      public static void LastLogWarningsShouldMatch(IDatabase database, string[] expectedLogs)
      {
         while (database.Warnings is null)
         {
            Thread.Sleep(200);
         }

         IWarning logWarning = database.Warnings.First(x => x.WarningType == WarningType.LogReviewWarning);

         string[] actualLogs = logWarning.Logs
            .OrderByDescending(x => x.DateTime)
            .Select(x => $"{(x.NeedsReview ? "Warning" : "Information")} : {x.Message}").ToArray();

         _lastLogsShouldMatch(actualLogs, expectedLogs);
      }

      private static void _lastLogsShouldMatch(string[] actualLogs, string[] expectedLogs)
      {
         for (int i = expectedLogs.Length - 1; i >= 0; i--)
         {
            _ = actualLogs[i].Should().Be(expectedLogs[i]);
         }
      }
   }
}
