using FluentAssertions;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Events;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;
using Upsilon.Apps.Passkey.UnitTests.Fakes;
using Upsilon.Apps.Passkey.Utils;

namespace Upsilon.Apps.Passkey.UnitTests
{
   internal static class UnitTestsHelper
   {
      public static readonly int RANDOMIZED_TESTS_LOOP = 10;

      public static readonly ICryptographyCenter CryptographicCenter = new CryptographyCenter();
      public static readonly ISerializationCenter SerializationCenter = new JsonSerializationCenter();
      public static readonly IPasswordFactory PasswordFactory = new PasswordFactory();
      /// <summary>No network — use for vault create/open so warning scans stay fast.</summary>
      public static readonly IPasswordFactory FastPasswordFactory = new FakePasswordFactory();
      public static readonly IClipboardManager ClipboardManager = new ClipboardManager();
      public static readonly ISecretMemoryProtector SecretMemoryProtector = new SecretMemoryProtector();

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

      public static void WriteFileZipEntry(string zipFile, string fileEntry, string content)
      {
         using ZipArchive archive = ZipFile.Open(zipFile, ZipArchiveMode.Update, Encoding.UTF8);

         archive.GetEntry(fileEntry)?.Delete();

         ZipArchiveEntry entry = archive.CreateEntry(fileEntry);

         using Stream stream = entry.Open();
         using StreamWriter writer = new(stream, Encoding.UTF8);

         writer.Write(content);
      }

      // Reproduces the FileLocker pipeline for the (unencrypted) activity entry:
      // the stored content is base64(gzip(json)). This lets a test surgically
      // tamper with the activity log to exercise the integrity checks.
      public static void TamperActivityLogSignature(string databaseFile)
      {
         string json = _decompress(ReadFileZipEntry(databaseFile, "activity"));

         string tampered = Regex.Replace(json, "\"Signature\":\"[^\"]*\"", "\"Signature\":\"\"");

         WriteFileZipEntry(databaseFile, "activity", _compress(tampered));
      }

      // Drops one entry from the sealed log while leaving SealedCount untouched,
      // so the stored list becomes shorter than the count it claims to have
      // sealed: a rollback/truncation of the log.
      public static void TamperActivityLogTruncate(string databaseFile)
      {
         JsonNode node = _readActivityNode(databaseFile);
         JsonArray list = node["ActivityList"]!.AsArray();

         list.RemoveAt(list.Count - 1);

         _writeActivityNode(databaseFile, node);
      }

      // Swaps in an attacker-controlled key pair's public key. The private key
      // that anchors verification still lives in the tamper-proof database, so
      // the stored public key no longer matches it: a key substitution.
      public static void TamperActivityLogPublicKey(string databaseFile)
      {
         CryptographicCenter.GenerateRandomKeys(out string attackerPublicKey, out _);

         JsonNode node = _readActivityNode(databaseFile);
         node["PublicKey"] = attackerPublicKey;

         _writeActivityNode(databaseFile, node);
      }

      // Reorders two sealed entries, which changes the canonical content the seal
      // was computed over without adding or removing anything: a reordering.
      public static void TamperActivityLogReorder(string databaseFile)
      {
         JsonNode node = _readActivityNode(databaseFile);
         JsonArray list = node["ActivityList"]!.AsArray();

         string first = list[0]!.GetValue<string>();
         string second = list[1]!.GetValue<string>();
         list[0] = second;
         list[1] = first;

         _writeActivityNode(databaseFile, node);
      }

      // Reproduces the FileLocker pipeline for the (unencrypted) header entry:
      // the stored content is base64(gzip(json)). Lowers the work factor so
      // Open's KDF floor can be exercised without minting a whole weak vault.
      public static void TamperKdfHeaderIterations(string databaseFile, int iterations)
      {
         JsonNode node = JsonNode.Parse(_decompress(ReadFileZipEntry(databaseFile, "header")))!;
         node["Iterations"] = iterations;
         WriteFileZipEntry(databaseFile, "header", _compress(node.ToJsonString()));
      }

      // Replaces the encrypted database entry with opaque garbage so Login with
      // the correct passkeys fails the outer AEAD layer as CorruptedSourceException
      // rather than WrongPasswordException.
      public static void TamperDatabaseEntryCorrupt(string databaseFile)
      {
         WriteFileZipEntry(databaseFile, "database", Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));
      }

      private static JsonNode _readActivityNode(string databaseFile)
         => JsonNode.Parse(_decompress(ReadFileZipEntry(databaseFile, "activity")))!;

      private static void _writeActivityNode(string databaseFile, JsonNode node)
         => WriteFileZipEntry(databaseFile, "activity", _compress(node.ToJsonString()));

      private static string _compress(string text)
      {
         byte[] bytes = Encoding.UTF8.GetBytes(text);
         using MemoryStream msi = new(bytes);
         using MemoryStream mso = new();
         using (GZipStream gs = new(mso, CompressionLevel.SmallestSize))
         {
            msi.CopyTo(gs);
         }
         return Convert.ToBase64String(mso.ToArray());
      }

      private static string _decompress(string compressedText)
      {
         byte[] bytes = Convert.FromBase64String(compressedText);
         using MemoryStream msi = new(bytes);
         using MemoryStream mso = new();
         using (GZipStream gs = new(msi, CompressionMode.Decompress))
         {
            gs.CopyTo(mso);
         }
         return Encoding.UTF8.GetString(mso.ToArray());
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
            FastPasswordFactory,
            ClipboardManager,
            SecretMemoryProtector,
            databaseFile,
            username,
            passkeys);

         return database;
      }

      public static IDatabase OpenTestDatabase(string[] passkeys, out IAlert[] detectedAlerts, AutoSaveMergeBehavior mergeAutoSave = AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile, [CallerMemberName] string username = "")
      {
         string databaseFile = ComputeDatabaseFilePath(username);

         TaskCompletionSource<IAlert[]> scanDone = new();

         IDatabase database = Database.Open(CryptographicCenter,
            SerializationCenter,
            FastPasswordFactory,
            ClipboardManager,
            SecretMemoryProtector,
            databaseFile,
            username);

         database.AutoSaveDetected += (s, e) => { e.MergeBehavior = mergeAutoSave; };
         void OnScanCompleted(object? sender, EventArgs e)
            => _ = scanDone.TrySetResult(FlattenCoreAlerts(database));
         database.CoreAlertsScanCompleted += OnScanCompleted;

         foreach (string passkey in passkeys)
         {
            _ = database.Login(passkey);
         }

         if (database.User is null)
         {
            database.CoreAlertsScanCompleted -= OnScanCompleted;
            detectedAlerts = [];
            return database;
         }

         if (!scanDone.Task.Wait(TimeSpan.FromSeconds(30)))
         {
            database.CoreAlertsScanCompleted -= OnScanCompleted;
            throw new TimeoutException("Timed out waiting for CoreAlertsScanCompleted after login.");
         }

         database.CoreAlertsScanCompleted -= OnScanCompleted;
         detectedAlerts = scanDone.Task.Result;

         return database;
      }

      public static IAlert[] FlattenCoreAlerts(IDatabase database)
         => [.. database.CoreAlerts.Values.SelectMany(static list => list)];

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

      public static string GetRandomString(int min = 16, int max = 0)
      {
         if (max == 0)
         {
            max = min + 10;
         }

         int length = GetRandomInt(min, max);

         // Ensure SecretQuality passes (length + mixed classes) so vault tests
         // are not flooded by WeakPasskey / WeakAccountPassword noise.
         const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
         char[] chars = new char[length];
         chars[0] = 'A';
         chars[1] = 'a';
         chars[2] = '2';
         chars[3] = '!';
         for (int i = 4; i < length; i++)
         {
            chars[i] = alphabet[GetRandomInt(0, alphabet.Length - 1)];
         }

         return new string(chars);
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

      /// <summary>
      /// Subscribes to the kind-specific Core alert event and
      /// <see cref="IDatabase.CoreAlertsScanCompleted"/>, then runs
      /// <paramref name="trigger"/> (typically <see cref="IDatabase.Save"/>) and
      /// waits until an alert of <paramref name="kind"/> is reported.
      /// </summary>
      public static IAlert[] WaitForAlertKind(IDatabase database, string kind, Action trigger, TimeSpan? timeout = null)
      {
         timeout ??= TimeSpan.FromSeconds(15);
         TaskCompletionSource<IAlert[]> tcs = new();

         void TryCompleteFromKind(IReadOnlyList<IAlert> reported)
         {
            if (reported.Count > 0)
            {
               _ = tcs.TrySetResult([.. reported]);
            }
         }

         void KindHandler(object? sender, AlertsChangedEventArgs e)
         {
            if (string.Equals(e.Kind, kind, StringComparison.Ordinal))
            {
               TryCompleteFromKind(e.Alerts);
            }
         }

         void ScanCompleted(object? sender, EventArgs e)
         {
            if (database.CoreAlerts.TryGetValue(kind, out IReadOnlyList<IAlert>? current)
               && current.Count > 0)
            {
               TryCompleteFromKind(current);
            }
         }

         EventHandler<AlertsChangedEventArgs>? kindSubscription = KindHandler;
         _subscribeKindChanged(database, kind, kindSubscription);
         database.CoreAlertsScanCompleted += ScanCompleted;

         try
         {
            // Do not complete from a pre-trigger CoreAlerts snapshot: Create/login
            // may already have published a stale VaultSecuritySettings (default
            // timeouts are 0) before the test mutates settings/accounts.
            trigger();

            if (!tcs.Task.Wait(timeout.Value))
            {
               throw new TimeoutException($"Timed out waiting for alert kind '{kind}'.");
            }

            return tcs.Task.Result;
         }
         finally
         {
            _unsubscribeKindChanged(database, kind, kindSubscription);
            database.CoreAlertsScanCompleted -= ScanCompleted;
         }
      }

      /// <summary>
      /// Subscribes to <see cref="IDatabase.CoreAlertsScanCompleted"/> then runs
      /// <paramref name="trigger"/> and waits for the next scan to finish,
      /// returning a flattened <see cref="IDatabase.CoreAlerts"/> snapshot.
      /// </summary>
      public static IAlert[] WaitForAlerts(IDatabase database, Action trigger, TimeSpan? timeout = null)
      {
         timeout ??= TimeSpan.FromSeconds(15);
         TaskCompletionSource<IAlert[]> tcs = new();

         void Handler(object? sender, EventArgs e)
            => _ = tcs.TrySetResult(FlattenCoreAlerts(database));

         database.CoreAlertsScanCompleted += Handler;

         try
         {
            trigger();

            if (!tcs.Task.Wait(timeout.Value))
            {
               throw new TimeoutException("Timed out waiting for a Core alert scan.");
            }

            return tcs.Task.Result;
         }
         finally
         {
            database.CoreAlertsScanCompleted -= Handler;
         }
      }

      private static void _subscribeKindChanged(IDatabase database, string kind, EventHandler<AlertsChangedEventArgs> handler)
      {
         switch (kind)
         {
            case AlertKinds.ActivityReview:
               database.ActivityReviewAlertsChanged += handler;
               break;
            case AlertKinds.PasswordUpdateReminder:
               database.PasswordUpdateReminderAlertsChanged += handler;
               break;
            case AlertKinds.DuplicatedPasswords:
               database.DuplicatedPasswordsAlertsChanged += handler;
               break;
            case AlertKinds.PasswordLeaked:
               database.PasswordLeakedAlertsChanged += handler;
               break;
            case AlertKinds.VaultSecuritySettings:
               database.VaultSecuritySettingsAlertsChanged += handler;
               break;
            case AlertKinds.InsufficientPasskeys:
               database.InsufficientPasskeysAlertsChanged += handler;
               break;
            case AlertKinds.WeakPasskey:
               database.WeakPasskeyAlertsChanged += handler;
               break;
            case AlertKinds.PasskeyLeaked:
               database.PasskeyLeakedAlertsChanged += handler;
               break;
            case AlertKinds.WeakAccountPassword:
               database.WeakAccountPasswordAlertsChanged += handler;
               break;
            case AlertKinds.PasskeyReusedAsAccountPassword:
               database.PasskeyReuseAlertsChanged += handler;
               break;
         }
      }

      private static void _unsubscribeKindChanged(IDatabase database, string kind, EventHandler<AlertsChangedEventArgs> handler)
      {
         switch (kind)
         {
            case AlertKinds.ActivityReview:
               database.ActivityReviewAlertsChanged -= handler;
               break;
            case AlertKinds.PasswordUpdateReminder:
               database.PasswordUpdateReminderAlertsChanged -= handler;
               break;
            case AlertKinds.DuplicatedPasswords:
               database.DuplicatedPasswordsAlertsChanged -= handler;
               break;
            case AlertKinds.PasswordLeaked:
               database.PasswordLeakedAlertsChanged -= handler;
               break;
            case AlertKinds.VaultSecuritySettings:
               database.VaultSecuritySettingsAlertsChanged -= handler;
               break;
            case AlertKinds.InsufficientPasskeys:
               database.InsufficientPasskeysAlertsChanged -= handler;
               break;
            case AlertKinds.WeakPasskey:
               database.WeakPasskeyAlertsChanged -= handler;
               break;
            case AlertKinds.PasskeyLeaked:
               database.PasskeyLeakedAlertsChanged -= handler;
               break;
            case AlertKinds.WeakAccountPassword:
               database.WeakAccountPasswordAlertsChanged -= handler;
               break;
            case AlertKinds.PasskeyReusedAsAccountPassword:
               database.PasskeyReuseAlertsChanged -= handler;
               break;
         }
      }

      public static void LastActivitiesShouldMatch(IDatabase database, string[] expectedActivities)
      {
         string[] actualActivities = database.Activities
            .Select(x => new ActivityViewModel(x))
            .Select(x => $"{(x.NeedsReview ? "Warning" : "Information")} : {x.Message}").ToArray();

         _lastActivitiesShouldMatch(actualActivities, expectedActivities);
      }

      /// <summary>
      /// Builds an expected activity line using the same localization path as <see cref="ActivityViewModel"/>.
      /// </summary>
      public static string FormatActivityLine(bool needsReview, string message)
      {
         message = message.Trim();
         message = message[..1].ToUpperInvariant() + message[1..];
         return $"{(needsReview ? "Warning" : "Information")} : {message}";
      }

      public static string FormatImportStarted(string filePath)
         => FormatActivityLine(true, Strings.Format(nameof(Strings.Activity_ImportingDataStarted), filePath));

      public static string FormatImportSucceeded()
         => FormatActivityLine(true, Strings.Activity_ImportingDataSucceded);

      public static string FormatImportFailed(ImportExportError error)
      {
         string reason = EnumDisplayHelper.FormatFieldValue(nameof(ImportExportError), error.ToString());
         return FormatActivityLine(true, Strings.Format(nameof(Strings.Activity_ImportingDataFailed), reason));
      }

      public static string FormatExportStarted(string filePath)
         => FormatActivityLine(true, Strings.Format(nameof(Strings.Activity_ExportingDataStarted), filePath));

      public static string FormatExportSucceeded()
         => FormatActivityLine(true, Strings.Activity_ExportingDataSucceded);

      public static string FormatExportFailed(ImportExportError error)
      {
         string reason = EnumDisplayHelper.FormatFieldValue(nameof(ImportExportError), error.ToString());
         return FormatActivityLine(true, Strings.Format(nameof(Strings.Activity_ExportingDataFailed), reason));
      }

      public static void LastActivityAlertsShouldMatch(IDatabase database, string[] expectedActivities)
      {
         DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
         IActivityReviewAlert? activityAlert = null;

         while (DateTime.UtcNow < deadline)
         {
            if (database.CoreAlerts.TryGetValue(AlertKinds.ActivityReview, out IReadOnlyList<IAlert>? list)
               && list.OfType<IActivityReviewAlert>().FirstOrDefault() is { } found)
            {
               activityAlert = found;
               break;
            }

            Thread.Sleep(200);
         }

         _ = activityAlert.Should().NotBeNull("ActivityReview alerts should be available");

         string[] actualActivities = activityAlert!.Activities
            .Select(x => new ActivityViewModel(x))
            .Select(x => $"{(x.NeedsReview ? "Warning" : "Information")} : {x.Message}").ToArray();

         _lastActivitiesShouldMatch(actualActivities, expectedActivities);
      }

      private static void _lastActivitiesShouldMatch(string[] actualActivities, string[] expectedActivities)
      {
         for (int i = expectedActivities.Length - 1; i >= 0; i--)
         {
            _ = actualActivities[i].Should().Be(expectedActivities[i]);
         }
      }
   }
}
