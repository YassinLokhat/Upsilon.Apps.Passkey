using FluentAssertions;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Upsilon.Apps.Passkey.Core.Public.Enums;
using Upsilon.Apps.Passkey.Core.Public.Interfaces;
using Upsilon.Apps.Passkey.Core.Public.Utils;

namespace Upsilon.Apps.Passkey.UnitTests
{
   internal static class UnitTestsHelper
   {
      public static readonly int RANDOMIZED_TESTS_LOOP = 100;

      public static readonly ICryptographyCenter CryptographicCenter = new CryptographyCenter();
      public static readonly ISerializationCenter SerializationCenter = new JsonSerializationCenter();
      public static readonly IPasswordFactory PasswordFactory = new PasswordFactory();

      public static string ComputeDatabaseFileDirectory([CallerMemberName] string username = "") => $"./TestFiles/{username}/{CryptographicCenter.GetHash(username)}";
      public static string ComputeDatabaseFilePath([CallerMemberName] string username = "") => $"{ComputeDatabaseFileDirectory(username)}/{CryptographicCenter.GetHash(username)}.pku";
      public static string ComputeAutoSaveFilePath([CallerMemberName] string username = "") => $"{ComputeDatabaseFileDirectory(username)}/{CryptographicCenter.GetHash(username)}.pka";
      public static string ComputeLogFilePath([CallerMemberName] string username = "") => $"{ComputeDatabaseFileDirectory(username)}/{CryptographicCenter.GetHash(username)}.pkl";

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
         string autoSaveFile = ComputeAutoSaveFilePath(username);
         string logFile = ComputeLogFilePath(username);

         passkeys ??= GetRandomStringArray();

         IDatabase database = IDatabase.Create(CryptographicCenter,
            SerializationCenter,
            PasswordFactory,
            databaseFile,
            autoSaveFile,
            logFile,
            username,
            passkeys);

         return database;
      }

      public static IDatabase OpenTestDatabase(string[] passkeys, out IWarning[] detectedWarnings, AutoSaveMergeBehavior mergeAutoSave = AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile, [CallerMemberName] string username = "")
      {
         string databaseFile = ComputeDatabaseFilePath(username);
         string autoSaveFile = ComputeAutoSaveFilePath(username);
         string logFile = ComputeLogFilePath(username);

         IWarning[] warnings = [];

         IDatabase database = IDatabase.Open(CryptographicCenter,
            SerializationCenter,
            PasswordFactory,
            databaseFile,
            autoSaveFile,
            logFile,
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
         string directory = ComputeDatabaseFileDirectory(username);

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
         while (database.Warnings == null)
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
