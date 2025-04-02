using System;
using System.Collections.Generic;
using k8s.Models;

namespace KubeStatus.Models
{
    public class Pod
    {
        public string? Name { get; set; }
        public string? Instance { get; set; }
        public string? Version { get; set; }
        public string? Component { get; set; }
        public string? PartOf { get; set; }
        public string? ManagedBy { get; set; }
        public string? CreatedBy { get; set; }
        public string? PodName { get; set; }
        public string? Namespace { get; set; }
        public IDictionary<string, string>? Labels { get; set; }
        public IDictionary<string, string>? Annotations { get; set; }
        public V1Affinity? Affinity { get; set; }
        public IList<V1Toleration>? Tolerations { get; set; }
        public string? PodStatus { get; set; }
        public string? PodStatusMessage { get; set; }
        public string? PodStatusReason { get; set; }
        public string? PodStatusPhase { get; set; }
        public IList<V1PodCondition>? PodConditions { get; set; }
        public DateTime? PodCreated { get; set; }
        public IList<string>? PodVolumes { get; set; }
        public IList<string>? PodIPs { get; set; }
        public string? HostIP { get; set; }
        public string? NodeName { get; set; }
        public IList<V1ContainerAndStatus>? InitContainers { get; set; }
        public IList<V1ContainerAndStatus>? Containers { get; set; }
        public IList<Corev1Event>? Events { get; set; }
    }

    public class V1ContainerAndStatus
    {
        public V1ContainerStatus? ContainerStatus { get; set; }
        public V1Container? Container { get; set; }
    }
}
