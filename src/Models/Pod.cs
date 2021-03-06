using System.Collections.Generic;
using k8s.Models;

namespace KubeStatus.Models
{
    public class Pod
    {
        public string Name { get; set; }
        public string Instance { get; set; }
        public string Version { get; set; }
        public string Component { get; set; }
        public string PartOf { get; set; }
        public string ManagedBy { get; set; }
        public string CreatedBy { get; set; }
        public string PodName { get; set; }
        public string Namespace { get; set; }
        public IDictionary<string, string> Labels { get; set; }
        public IDictionary<string, string> Annotations { get; set; }
        public IList<V1ContainerStatus> Status { get; set; }
        public string PodStatus { get; set; }
        public IList<string> PodVolumes { get; set; }
    }
}