using FluentAssertions;
using Upsilon.Apps.Passkey.Core.Utils;

namespace Upsilon.Apps.Passkey.UnitTests.Utils
{
   [TestClass]
   public sealed class DeferredPersistenceUnitTests
   {
      [TestMethod]
      /*
       * Multiple Schedule calls coalesce into a single persist when Flush runs.
      */
      public void Case01_ScheduleCoalescesIntoSingleFlush()
      {
         int persistCount = 0;
         using DeferredPersistence deferred = new(() => persistCount++);

         deferred.Schedule();
         deferred.Schedule();
         deferred.Schedule();
         deferred.Flush();

         _ = persistCount.Should().Be(1);
      }

      [TestMethod]
      /*
       * Flush is a no-op when nothing is dirty.
      */
      public void Case02_FlushWhenCleanDoesNotPersist()
      {
         int persistCount = 0;
         using DeferredPersistence deferred = new(() => persistCount++);

         deferred.Flush();

         _ = persistCount.Should().Be(0);
      }

      [TestMethod]
      /*
       * Cancel drops pending work so a later Flush does not write.
      */
      public void Case03_CancelDropsPendingWork()
      {
         int persistCount = 0;
         using DeferredPersistence deferred = new(() => persistCount++);

         deferred.Schedule();
         deferred.Cancel();
         deferred.Flush();

         _ = persistCount.Should().Be(0);
      }

      [TestMethod]
      /*
       * Dispose drops dirty work without calling persist (callers must Flush first).
      */
      public void Case04_DisposeDropsDirtyWithoutPersisting()
      {
         int persistCount = 0;
         DeferredPersistence deferred = new(() => persistCount++);

         deferred.Schedule();
         deferred.Dispose();
         deferred.Flush();

         _ = persistCount.Should().Be(0);
      }

      [TestMethod]
      /*
       * Schedule after Dispose throws ObjectDisposedException.
      */
      public void Case05_ScheduleAfterDisposeThrows()
      {
         DeferredPersistence deferred = new(() => { });
         deferred.Dispose();

         Action schedule = () => deferred.Schedule();
         schedule.Should().Throw<ObjectDisposedException>();
      }

      [TestMethod]
      /*
       * When an explicit Flush fails, dirty is re-armed so a later Flush retries.
      */
      public void Case06_FailedFlushRearmsDirtyForLaterRetry()
      {
         int persistCount = 0;
         bool failOnce = true;
         using DeferredPersistence deferred = new(() =>
         {
            persistCount++;
            if (failOnce)
            {
               failOnce = false;
               throw new InvalidOperationException("simulated disk failure");
            }
         });

         deferred.Schedule();

         Action firstFlush = () => deferred.Flush();
         firstFlush.Should().Throw<InvalidOperationException>();
         _ = persistCount.Should().Be(1);

         deferred.Flush();
         _ = persistCount.Should().Be(2);
      }

      [TestMethod]
      /*
       * A later Schedule after a successful Flush arms a new persist.
      */
      public void Case07_ScheduleAfterFlushPersistsAgain()
      {
         int persistCount = 0;
         using DeferredPersistence deferred = new(() => persistCount++);

         deferred.Schedule();
         deferred.Flush();
         deferred.Schedule();
         deferred.Flush();

         _ = persistCount.Should().Be(2);
      }
   }
}
