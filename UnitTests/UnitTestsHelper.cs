using System.Runtime.CompilerServices;
using Upsilon.Apps.PassKey.Core.Enums;
using Upsilon.Apps.PassKey.Core.Interfaces;
using Upsilon.Apps.PassKey.Core.Utils;

namespace Upsilon.Apps.PassKey.UnitTests
{
   internal static class UnitTestsHelper
   {
      public static readonly int RANDOMIZED_TESTS_LOOP = 100;

      public static readonly ICryptographyCenter CryptographicCenter = new CryptographyCenter();
      public static readonly ISerializationCenter SerializationCenter = new JsonSerializationCenter();

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

         IDatabase database = IDatabase.Create(CryptographicCenter, SerializationCenter, databaseFile, autoSaveFile, logFile, username, passkeys);

         foreach (string passkey in passkeys)
         {
            _ = database.Login(passkey);
         }

         return database;
      }

      public static IDatabase OpenTestDatabase(string[] passkeys, AutoSaveMergeBehavior mergeAutoSave = AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile, [CallerMemberName] string username = "")
      {
         string databaseFile = ComputeDatabaseFilePath(username);
         string autoSaveFile = ComputeAutoSaveFilePath(username);
         string logFile = ComputeLogFilePath(username);

         IDatabase database = IDatabase.Open(CryptographicCenter, SerializationCenter, databaseFile, autoSaveFile, logFile, username, (s, e) => { e.MergeBehavior = mergeAutoSave; });

         foreach (string passkey in passkeys)
         {
            _ = database.Login(passkey);
         }

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

      private static Random _getRandom() => new((int)DateTime.Now.Ticks);

      public static string[] GetRandomStringArray(int count = 0)
      {
         Random random = _getRandom();

         if (count == 0)
         {
            count = random.Next(2, 5);
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
         Random random = _getRandom();

         if (max == 0)
         {
            max = min + 10;
         }

         int length = random.Next(min, max);

         byte[] bytes = new byte[length];

         random.NextBytes(bytes);

         return Convert.ToBase64String(bytes)[..length];
      }

      public static int GetRandomInt(int max) => GetRandomInt(0, max);

      public static int GetRandomInt(int min, int max)
      {
         Random random = _getRandom();

         return random.Next(min, max);
      }

      public static bool LastLogsMatches(IDatabase database, string[] expectedLogs)
      {
         string[] actualLogs = database.Logs.Select(x => $"{x.ItemName}|{x.Message}|{x.NeedsReview}").ToArray();

         for (int i = 0; i < expectedLogs.Length; i++)
         {
            if (expectedLogs[i] != actualLogs[i])
               return false;
         }

         return true;
      }
   }
}
