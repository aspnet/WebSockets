// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.CommandLineUtils;

namespace MiniBench
{
    public class BenchmarkOptions
    {
        public static readonly int DefaultPipelineDepth = 1;
        public static readonly int DefaultWarmupIterations = 10;
        public static readonly TimeSpan DefaultTestLength = TimeSpan.FromSeconds(10);
        public static readonly int DefaultTestRuns = 1;

        private CommandOption _pipelineDepth;
        private CommandOption _testLength;
        private CommandOption _warmupIterations;
        private CommandOption _testRuns;

        public int PipelineDepth => _pipelineDepth.HasValue() ? int.Parse(_pipelineDepth.Value()) : DefaultPipelineDepth;

        public int WarmupIterations => _warmupIterations.HasValue() ? int.Parse(_warmupIterations.Value()) : DefaultWarmupIterations;

        public int TestRuns => _testRuns.HasValue() ? int.Parse(_testRuns.Value()) : DefaultTestRuns;

        public TimeSpan TestLength => _testLength.HasValue() ? TimeSpan.FromSeconds(int.Parse(_testLength.Value())) : DefaultTestLength;

        public BenchmarkOptions(CommandOption pipelineDepth, CommandOption warmupIterations, CommandOption testLength, CommandOption testRuns)
        {
            _pipelineDepth = pipelineDepth;
            _warmupIterations = warmupIterations;
            _testLength = testLength;
            _testRuns = testRuns;
        }

        public static BenchmarkOptions Attach(CommandLineApplication cmd)
        {
            var pipelineDepth = cmd.Option("-p|--pipeline-depth <DEPTH>", "The number of concurrent WebSocket messages to send before waiting for responses (Default: 1)", CommandOptionType.SingleValue);
            var warmupIterations = cmd.Option("-w|--warmup <ITERATIONS>", "The number of iterations to run before starting MPS calculations", CommandOptionType.SingleValue);
            var testLength = cmd.Option("-d|--duration <LENGTH>", "The duration of the test, in seconds.", CommandOptionType.SingleValue);
            var testRuns = cmd.Option("-r|--runs <RUNS>", "The number of consecutive runs to perform.", CommandOptionType.SingleValue);

            return new BenchmarkOptions(pipelineDepth, warmupIterations, testLength, testRuns);
        }
    }
}