using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MiniBench
{
    public abstract class Scenario : IDisposable
    {
        public abstract string FullName { get; }
        public abstract string Name { get; }

        public abstract Task<ScenarioResult> Run(TextWriter output, BenchmarkOptions benchmarkOptions, CancellationToken cancellationToken);
        public abstract Task Initialize(TextWriter output, CancellationToken cancellationToken);
        public abstract void Dispose();
    }
}