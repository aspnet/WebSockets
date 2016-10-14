using System;

namespace MiniBench
{
    public class ScenarioResult
    {
        public string Scenario { get; }
        public int PipelineDepth { get; }
        public DateTime StartTimeUtc { get; }
        public TimeSpan IntendedDuration { get; }
        public TimeSpan ActualDuration { get; }
        public int MessagesSent { get; }
        public double MessagesPerSecond { get; }

        public ScenarioResult(string scenario, int pipelineDepth, DateTime startTimeUtc, TimeSpan intendedDuration, TimeSpan actualDuration, int messages)
        {
            Scenario = scenario;
            PipelineDepth = pipelineDepth;
            StartTimeUtc = startTimeUtc;
            IntendedDuration = intendedDuration;
            ActualDuration = actualDuration;
            MessagesSent = messages;
            MessagesPerSecond = messages / actualDuration.TotalSeconds;
        }
    }
}
