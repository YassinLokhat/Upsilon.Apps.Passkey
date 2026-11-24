using FluentAssertions;
using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;

namespace UnitTests.Presentation
{
   [TestClass]
   public sealed class FoundationTests
   {
      [TestMethod]
      /*
       * While an execution is pending the command reports CanExecute = false
       * (re-entrancy guard), raising CanExecuteChanged on start and on end.
      */
      public async Task Case01_AsyncRelayCommand_BlocksReentrancyWhileRunning()
      {
         TaskCompletionSource gate = new();
         int runs = 0;
         int changes = 0;
         AsyncRelayCommand command = new(async () =>
         {
            runs++;
            await gate.Task.ConfigureAwait(false);
         });
         command.CanExecuteChanged += (_, _) => changes++;

         Task first = command.ExecuteAsync(null);
         _ = command.IsRunning.Should().BeTrue();
         _ = command.CanExecute(null).Should().BeFalse();

         await command.ExecuteAsync(null);
         _ = runs.Should().Be(1);

         gate.SetResult();
         await first;

         _ = command.IsRunning.Should().BeFalse();
         _ = command.CanExecute(null).Should().BeTrue();
         _ = changes.Should().Be(2);
      }

      [TestMethod]
      /*
       * The predicate is honoured, and a failing execution still releases the
       * running flag so the command can be retried.
      */
      public async Task Case02_AsyncRelayCommand_PredicateAndFailureRelease()
      {
         bool allowed = false;
         AsyncRelayCommand command = new(() => throw new InvalidOperationException("boom"), () => allowed);

         _ = command.CanExecute(null).Should().BeFalse();
         await command.ExecuteAsync(null);

         allowed = true;
         Func<Task> run = () => command.ExecuteAsync(null);
         _ = await run.Should().ThrowAsync<InvalidOperationException>();
         _ = command.IsRunning.Should().BeFalse();
         _ = command.CanExecute(null).Should().BeTrue();
      }

      [TestMethod]
      public void Case03_RelayCommand_NotifiesExplicitly()
      {
         int changes = 0;
         RelayCommand first = new(() => { });
         RelayCommand second = new(() => { }, () => false);
         first.CanExecuteChanged += (_, _) => changes++;
         second.CanExecuteChanged += (_, _) => changes++;

         RelayCommand.NotifyCanExecuteChanged(first, second);

         _ = changes.Should().Be(2);
         _ = second.CanExecute(null).Should().BeFalse();
      }

      [TestMethod]
      public void Case04_AssemblyAppInfo_BuildsTitleFromNameAndVersion()
      {
         AssemblyAppInfo info = new(typeof(AppServices).Assembly);

         _ = info.Name.Should().Be("Upsilon.Apps.Passkey.GUI.Presentation");
         _ = info.Version.Should().MatchRegex(@"^\d+\.\d+\.\d+$");
         _ = info.Title.Should().Be($"{info.Name} v{info.Version}");
      }

      [TestMethod]
      /*
       * The log writes a daily file in the given directory and never throws.
      */
      public void Case05_TraceFileAppLog_WritesDailyFile()
      {
         string directory = Path.Join(Path.GetTempPath(), $"pk-log-{Guid.NewGuid():N}");
         try
         {
            using (TraceFileAppLog log = new(directory))
            {
               log.Info("info line");
               log.Warn("warn line");
               log.Error("error line", new InvalidOperationException("detail"));
            }

            string file = Directory.GetFiles(directory, "app-*.log").Single();
            string content = File.ReadAllText(file);
            _ = content.Should().Contain("info line").And.Contain("warn line").And.Contain("error line");
         }
         finally
         {
            if (Directory.Exists(directory))
            {
               Directory.Delete(directory, recursive: true);
            }
         }
      }
   }
}
