using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace KubeStatus.Models
{
    public class Metadata
    {
        [JsonProperty("annotations")]
        public IDictionary<string, string> Annotations { get; set; }

        [JsonProperty("creationTimestamp")]
        public DateTimeOffset CreationTimestamp { get; set; }

        [JsonProperty("generation")]
        public long Generation { get; set; }

        [JsonProperty("labels")]
        public IDictionary<string, string> Labels { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("namespace")]
        public string Namespace { get; set; }
    }
}
