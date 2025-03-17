using System;
using System.Collections.Generic;

namespace KubeStatus.Models
{
    public class Metadata
    {
        public IDictionary<string, string>? Annotations { get; set; }
        public DateTimeOffset CreationTimestamp { get; set; }
        public long Generation { get; set; }
        public IDictionary<string, string>? Labels { get; set; }
        public string? Name { get; set; }
        public string? Namespace { get; set; }
    }
}
