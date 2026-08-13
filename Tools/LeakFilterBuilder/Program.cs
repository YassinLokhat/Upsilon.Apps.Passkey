using Upsilon.Apps.Passkey.Core.Utils.LeakFilter;

namespace Upsilon.Apps.Passkey.Tools.LeakFilterBuilder
{
   internal static class Program
   {
      private static async Task<int> Main(string[] args)
      {
         if (args.Length == 0 || args.Any(a => a is "-h" or "--help" or "/?"))
         {
            _printHelp();
            return args.Length == 0 ? 1 : 0;
         }

         if (!string.Equals(args[0], "build", StringComparison.OrdinalIgnoreCase))
         {
            Console.Error.WriteLine($"Unknown command '{args[0]}'.");
            _printHelp();
            return 1;
         }

         string output = LeakFilterPaths.FilterFilePath;
         bool force = false;
         int parallelism = 32;
         ulong capacity = BloomSizing.DefaultCapacity;
         double falsePositiveRate = BloomSizing.DefaultFalsePositiveRate;

         for (int i = 1; i < args.Length; i++)
         {
            string arg = args[i];
            if (arg is "--force" or "-f")
            {
               force = true;
               continue;
            }

            if ((arg is "--out" or "-o") && i + 1 < args.Length)
            {
               output = args[++i];
               continue;
            }

            if ((arg is "--parallel" or "-p") && i + 1 < args.Length
               && int.TryParse(args[++i], out int parsedParallel))
            {
               parallelism = parsedParallel;
               continue;
            }

            if (arg is "--capacity" && i + 1 < args.Length
               && ulong.TryParse(args[++i], out ulong parsedCapacity))
            {
               capacity = parsedCapacity;
               continue;
            }

            if (arg is "--fpr" && i + 1 < args.Length
               && double.TryParse(args[++i], System.Globalization.NumberStyles.Float,
                  System.Globalization.CultureInfo.InvariantCulture, out double parsedFpr))
            {
               falsePositiveRate = parsedFpr;
               continue;
            }

            Console.Error.WriteLine($"Unrecognized argument '{arg}'.");
            _printHelp();
            return 1;
         }

         Console.WriteLine($"Building offline HIBP Bloom filter → {output}");
         Console.WriteLine($"capacity={capacity}, fpr={falsePositiveRate}, parallel={parallelism}, force={force}");
         Console.WriteLine("This can take several hours and tens of GB of network transfer.");

         Progress<HibpBloomBuildProgress> progress = new(p =>
         {
            if (p.Skipped)
            {
               Console.WriteLine("Skipped: filter file already exists (pass --force to rebuild).");
               return;
            }

            double pct = 100.0 * p.CompletedPrefixes / p.TotalPrefixes;
            Console.WriteLine(
               $"[{pct,6:0.00}%] prefixes {p.CompletedPrefixes}/{p.TotalPrefixes}, hashes inserted ≈ {p.InsertedHashes}");
         });

         try
         {
            HibpBloomBuildResult result = await HibpBloomBuilder.BuildAsync(
               output,
               capacity,
               falsePositiveRate,
               parallelism,
               force,
               progress).ConfigureAwait(false);

            if (result.Skipped)
            {
               return 0;
            }

            Console.WriteLine($"Done. InsertedCount={result.InsertedCount}, BuiltUtc={result.BuiltUtc:O}");
            Console.WriteLine($"Filter: {result.OutputPath}");
            return 0;
         }
         catch (Exception ex)
         {
            Console.Error.WriteLine(ex);
            return 2;
         }
      }

      private static void _printHelp()
      {
         Console.WriteLine(
            """
            Passkey.LeakFilterBuilder — build the offline HIBP SHA-1 Bloom filter (.pkbf)

            Usage:
              Passkey.LeakFilterBuilder build [options]

            Options:
              -o, --out <path>       Output .pkbf path (default: LeakFilterPaths root / pwned-sha1.pkbf;
                                     Core default root is %LocalAppData%\Passkey)
              -f, --force            Rebuild even if the file already exists
              -p, --parallel <n>     Concurrent HIBP range downloads (default: 32)
                  --capacity <n>     Expected hash count for sizing (default: 2100000000)
                  --fpr <rate>       Target false-positive rate (default: 0.01)
              -h, --help             Show this help

            Notes:
              - Application-level file: shared by all vault users on this machine.
              - Disabling the filter in the app never deletes this file.
            """);
      }
   }
}
