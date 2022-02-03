using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace KubeStatus.Models
{
    public class SparkApplication
    {
        [JsonProperty("apiVersion")]
        public string ApiVersion { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }

        [JsonProperty("status")]
        public SparkApplicationStatus Status { get; set; }

        [JsonProperty("spec")]
        public string Spec { get; set; }
    }

    public partial class SparkApplicationStatus
    {
        [JsonProperty("applicationState")]
        public Dictionary<string, string> ApplicationState { get; set; }

        [JsonProperty("driverInfo")]
        public Dictionary<string, string> DriverInfo { get; set; }

        [JsonProperty("executionAttempts")]
        public long? ExecutionAttempts { get; set; }

        [JsonProperty("executorState")]
        public Dictionary<string, string> ExecutorState { get; set; }

        [JsonProperty("lastSubmissionAttemptTime")]
        public DateTimeOffset? LastSubmissionAttemptTime { get; set; }

        [JsonProperty("sparkApplicationId")]
        public string SparkApplicationId { get; set; }

        [JsonProperty("submissionAttempts")]
        public long? SubmissionAttempts { get; set; }

        [JsonProperty("submissionID")]
        public Guid? SubmissionId { get; set; }

        [JsonProperty("terminationTime")]
        public DateTimeOffset? TerminationTime { get; set; }
    }
}
