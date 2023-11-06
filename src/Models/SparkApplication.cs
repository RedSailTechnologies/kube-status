using System;
using System.Collections.Generic;

namespace KubeStatus.Models
{
    public class SparkApplication
    {
        public string ApiVersion { get; set; }
        public string Kind { get; set; }
        public Metadata Metadata { get; set; }
        public SparkApplicationStatus Status { get; set; }
        public string Spec { get; set; }
    }

    public partial class SparkApplicationStatus
    {
        public Dictionary<string, string> ApplicationState { get; set; }
        public Dictionary<string, object> DriverInfo { get; set; }
        public long? ExecutionAttempts { get; set; }
        public Dictionary<string, string> ExecutorState { get; set; }
        public DateTimeOffset? LastSubmissionAttemptTime { get; set; }
        public string SparkApplicationId { get; set; }
        public long? SubmissionAttempts { get; set; }
        public Guid? SubmissionId { get; set; }
        public DateTimeOffset? TerminationTime { get; set; }
    }
}
