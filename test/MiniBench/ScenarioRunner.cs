using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace MiniBench
{
    internal static class ScenarioRunner
    {
        internal static Action<CommandLineApplication> Create<T>(T scenario, CancellationToken cancellationToken) where T : Scenario
        {
            return cmd =>
            {
                cmd.Description = $"Run the {scenario.FullName} scenario";

                var benchmarkOptions = BenchmarkOptions.Attach(cmd);
                var reportingOptions = ReportingOptions.Attach(cmd);

                cmd.OnExecute(() => Run(benchmarkOptions, reportingOptions, scenario, cancellationToken));
            };
        }

        private static async Task<int> Run<T>(BenchmarkOptions benchmarkOptions, ReportingOptions reportingOptions, T scenario, CancellationToken cancellationToken) where T : Scenario
        {
            using (scenario)
            {
                try
                {
                    var results = new List<ScenarioResult>();

                    var output = reportingOptions.Quiet ? TextWriter.Null : Console.Out;
                    await scenario.Initialize(output, cancellationToken);

                    for (var i = 0; i < benchmarkOptions.TestRuns; i++)
                    {
                        results.Add(await scenario.Run(output, benchmarkOptions, cancellationToken));
                    }

                    foreach (var result in results)
                    {
                        await Report(result, reportingOptions);
                    }
                    return 0;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("error: " + ex.ToString());
                    return 1;
                }
            }
        }

        private static async Task Report(ScenarioResult results, ReportingOptions reportingOptions)
        {
            if (!reportingOptions.Quiet)
            {
                Console.WriteLine($"{results.MessagesSent} sent in {results.ActualDuration.TotalSeconds:0.00} seconds, {results.MessagesPerSecond:0.00} messages/second");
            }

            if (!string.IsNullOrEmpty(reportingOptions.Append))
            {
                Console.WriteLine("Appending results to: " + reportingOptions.Append);
                await Write(results, FileMode.Append, reportingOptions.Append);
            }
            else if(!string.IsNullOrEmpty(reportingOptions.Output))
            {
                Console.WriteLine("Writing results to: " + reportingOptions.Append);
                await Write(results, FileMode.Create, reportingOptions.Output);
            }
        }

        private static async Task Write(ScenarioResult results, FileMode mode, string path)
        {
            using (var stream = new FileStream(path, mode, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                // If we're at the start, write the header.
                if (stream.Position == 0)
                {
                    await writer.WriteLineAsync(
                        "Scenario," +
                        "PipelineDepth," +
                        "StartTimeUtc," +
                        "IntendedDuration(ms)," +
                        "ActualDuration(ms)," +
                        "MessagesSent," +
                        "MessagesPerSecond");
                }
                await writer.WriteLineAsync(
                    results.Scenario + "," +
                    results.PipelineDepth + "," +
                    results.StartTimeUtc.ToString("O") + "," +
                    results.IntendedDuration.TotalMilliseconds.ToString() + "," +
                    results.ActualDuration.TotalMilliseconds.ToString() + "," +
                    results.MessagesSent.ToString() + "," +
                    results.MessagesPerSecond.ToString());
            }
        }
    }
}
