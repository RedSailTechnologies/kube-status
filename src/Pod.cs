using System.Collections.Generic;

namespace KubeStatus
{
    public class Pod
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string Version { get; set; }
        public IDictionary<string, string> Labels { get; set; }
        public IDictionary<string, string> Annotations { get; set; }
        public IList<k8s.Models.V1ContainerStatus> Status { get; set; }
    }
}
