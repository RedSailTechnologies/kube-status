using System;

namespace KubeStatus.Models
{
    public class HelmListItem
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public int Revision { get; set; }
        public DateTimeOffset Updated { get; set; }
        public string Status { get; set; }
        public string Chart { get; set; }
        public string AppVersion { get; set; }
    }
}
