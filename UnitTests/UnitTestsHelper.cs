using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using FluentAssertions;
using Upsilon.Apps.PassKey.Core.Models;
using Upsilon.Apps.PassKey.Core.Utils;
using Upsilon.Apps.PassKey.Interfaces;
using Upsilon.Apps.PassKey.Interfaces.Enums;

namespace Upsilon.Apps.PassKey.UnitTests
{
   internal static class UnitTestsHelper
   {
      public static readonly int RANDOMIZED_TESTS_LOOP = 100;

      public static readonly ICryptographyCenter CryptographicCenter = new CryptographyCenter();
      public static readonly ISerializationCenter SerializationCenter = new JsonSerializationCenter();
      public static readonly IPasswordFactory PasswordFactory = new PasswordFactory();

      public static string ComputeDatabaseFileDirectory([CallerMemberName] string username = "") => $"./TestFiles/{username}";
      public static string ComputeDatabaseFilePath([CallerMemberName] string username = "") => $"{ComputeDatabaseFileDirectory(username)}/{username}.pku";
      public static string ComputeAutoSaveFilePath([CallerMemberName] string username = "") => $"{ComputeDatabaseFileDirectory(username)}/{username}.pka";
      public static string ComputeLogFilePath([CallerMemberName] string username = "") => $"{ComputeDatabaseFileDirectory(username)}/{username}.pkl";

      public static IDatabase CreateTestDatabase(string[] passkeys = null, [CallerMemberName] string username = "")
      {
         string databaseFile = ComputeDatabaseFilePath(username);
         string autoSaveFile = ComputeAutoSaveFilePath(username);
         string logFile = ComputeLogFilePath(username);

         passkeys ??= GetRandomStringArray();

         IDatabase database = Database.Create(CryptographicCenter,
            SerializationCenter,
            PasswordFactory,
            databaseFile,
            autoSaveFile,
            logFile,
            username,
            passkeys);

         foreach (string passkey in passkeys)
         {
            _ = database.Login(passkey);
         }

         return database;
      }

      public static IDatabase OpenTestDatabase(string[] passkeys, out IWarning[] detectedWarnings, AutoSaveMergeBehavior mergeAutoSave = AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile, [CallerMemberName] string username = "")
      {
         string databaseFile = ComputeDatabaseFilePath(username);
         string autoSaveFile = ComputeAutoSaveFilePath(username);
         string logFile = ComputeLogFilePath(username);

         IWarning[] warnings = [];

         IDatabase database = Database.Open(CryptographicCenter,
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
