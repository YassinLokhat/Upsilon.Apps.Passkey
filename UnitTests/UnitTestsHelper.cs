using System.Runtime.CompilerServices;
using System.Text;
using Upsilon.Apps.Passkey.Core.Interfaces;
using Upsilon.Apps.PassKey.Core.Utils;

namespace Upsilon.Apps.Passkey.UnitTests
{
   internal static class UnitTestsHelper
   {
      public static readonly int RANDOMIZED_TESTS_LOOP = 100;

      public static string ComputeDatabaseFileDirectory([CallerMemberName] string username = "") => $"./TestFiles/{username}";
      public static string ComputeDatabaseFilePath([CallerMemberName] string username = "") => $"{ComputeDatabaseFileDirectory(username)}/{username}.pku";
      public static string ComputeAutoSaveFilePath([CallerMemberName] string username = "") => $"{ComputeDatabaseFileDirectory(username)}/{username}.pka";
      public static string ComputeLogFilePath([CallerMemberName] string username = "") => $"{ComputeDatabaseFileDirectory(username)}/{username}.pkl";

      public static IDatabase CreateTestDatabase(string[]? passkeys = null, [CallerMemberName] string username = "")
      {
         string databaseFile = ComputeDatabaseFilePath(username);
         string autoSaveFile = ComputeAutoSaveFilePath(username);
         string logFile = ComputeLogFilePath(username);

         passkeys ??= GetRandomPasskeys();

         IDatabase database = IDatabase.Create(databaseFile, autoSaveFile, logFile, username, passkeys);

         foreach (string passkey in passkeys)
         {
            _ = database.Login(passkey);
         }

         return database;
      }

      public static IDatabase OpenTestDatabase(string[] passkeys, bool mergeAutoSave = false, [CallerMemberName] string username = "")
      {
         string databaseFile = ComputeDatabaseFilePath(username);
         string autoSaveFile = ComputeAutoSaveFilePath(username);
         string logFile = ComputeLogFilePath(username);

         IDatabase database = IDatabase.Open(databaseFile, autoSaveFile, logFile, username, (s, e) => { e.MergeAutoSave = mergeAutoSave; });

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

      public static string[] GetRandomPasskeys(int count = 0)
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

      public static string GetRandomString()
      {
         Random random = _getRandom();

         int length = random.Next(10, 20);

         byte[] bytes = new byte[length];

         random.NextBytes(bytes);

         return Encoding.ASCII.GetString(bytes).GetHash();
      }

      public static int GetRandomInt(int max) => GetRandomInt(0, max);

      public static int GetRandomInt(int min, int max)
      {
         Random random = _getRandom();

         return random.Next(min, max);
      }
   }
}
