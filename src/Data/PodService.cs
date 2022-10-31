using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeStatus.Models;
using Microsoft.Extensions.Caching.Memory;

namespace KubeStatus.Data
{
    public class PodService
    {
        private readonly IKubernetes kubernetesClient;

        public IMemoryCache MemoryCache { get; }

        public PodService(IKubernetes kubernetesClient, IMemoryCache memoryCache)
        {
            this.kubernetesClient = kubernetesClient;
            MemoryCache = memoryCache;
        }

        public Task<IEnumerable<Pod>> GetAllNamespacedPodsAsync(string k8sNamespace = "default")
        {
            return MemoryCache.GetOrCreateAsync(k8sNamespace, async e =>
            {
                e.SetOptions(new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromSeconds(10)
                });

                var namespaces = await kubernetesClient.CoreV1.ListNamespaceAsync();
                if (!namespaces.Items.Any(n => n.Metadata.Name.Equals(k8sNamespace, System.StringComparison.OrdinalIgnoreCase)))
                {
                    return null;
                }

                var list = await kubernetesClient.CoreV1.ListNamespacedPodAsync(k8sNamespace);
                var events = await kubernetesClient.CoreV1.ListNamespacedEventAsync(k8sNamespace);

                var pods = new List<Pod>();

                foreach (var item in list.Items)
                {
                    var podEvents = events.Items.Where(i => i.InvolvedObject.Uid.Equals(item.Metadata.Uid)).OrderByDescending(i => i.LastTimestamp).ToList();

                    pods.Add(new Pod
                    {
                        Name = item.Metadata.Labels.ContainsKey("app.kubernetes.io/name") ? item.Metadata.Labels["app.kubernetes.io/name"] : "",
                        Instance = item.Metadata.Labels.ContainsKey("app.kubernetes.io/instance") ? item.Metadata.Labels["app.kubernetes.io/instance"] : "",
                        Version = item.Metadata.Labels.ContainsKey("app.kubernetes.io/version") ? item.Metadata.Labels["app.kubernetes.io/version"] : "",
                        Component = item.Metadata.Labels.ContainsKey("app.kubernetes.io/component") ? item.Metadata.Labels["app.kubernetes.io/component"] : "",
                        PartOf = item.Metadata.Labels.ContainsKey("app.kubernetes.io/part-of") ? item.Metadata.Labels["app.kubernetes.io/part-of"] : "",
                        ManagedBy = item.Metadata.Labels.ContainsKey("app.kubernetes.io/managed-by") ? item.Metadata.Labels["app.kubernetes.io/managed-by"] : "",
                        CreatedBy = item.Metadata.Labels.ContainsKey("app.kubernetes.io/created-by") ? item.Metadata.Labels["app.kubernetes.io/created-by"] : "",
                        PodName = item.Metadata.Name,
                        Namespace = k8sNamespace,
                        Labels = item.Metadata.Labels,
                        Annotations = item.Metadata.Annotations,
                        Affinity = item.Spec.Affinity,
                        Tolerations = item.Spec.Tolerations,
                        PodStatus = item.Status.Phase,
                        PodStatusMessage = item.Status.Message,
                        PodStatusReason = item.Status.Reason,
                        PodStatusPhase = item.Status.Phase,
                        PodConditions = item.Status.Conditions,
                        PodCreated = item.Metadata.CreationTimestamp,
                        PodVolumes = item.Spec.Volumes?.Select(v => v.Name).ToList(),
                        PodIPs = item.Status.PodIPs?.Select(i => i.Ip).ToList(),
                        HostIP = item.Status.HostIP,
                        NodeName = item.Spec.NodeName,
                        InitContainers = SortContainersAndStatuses(item.Status.InitContainerStatuses, item.Spec.InitContainers),
                        Containers = SortContainersAndStatuses(item.Status.ContainerStatuses, item.Spec.Containers),
                        Events = podEvents
                    });
                }

                return await Task.FromResult(pods.AsEnumerable());
            });
        }

        private IList<V1ContainerAndStatus> SortContainersAndStatuses(IList<V1ContainerStatus> statuses, IList<V1Container> containers)
        {
            var list = new List<V1ContainerAndStatus>();

            if (containers != null && containers.Count > 0)
            {
                foreach (var container in containers.OrderBy(c => c.Name))
                {
                    var name = container.Name;
                    var status = statuses.Where(s => s.Name.Equals(name)).FirstOrDefault();
                    list.Add(new V1ContainerAndStatus { ContainerStatus = status, Container = container });
                }
            }
            else if (statuses != null && statuses.Count > 0)
            {
                foreach (var status in statuses.OrderBy(s => s.Name))
                {
                    list.Add(new V1ContainerAndStatus { ContainerStatus = status });
                }
            }

            return list;
        }

        public async Task<byte[]> GetAllNamespacedPodsFileAsync(string k8sNamespace = "default")
        {
            try
            {
                var pods = await GetAllNamespacedPodsAsync(k8sNamespace);
                if (pods == null)
                {
                    return null;
                }
                else
                {
                    byte[] bytesInStream;
                    var podList = pods
                        .Select(p => new PodMeta
                        {
                            Name = string.IsNullOrWhiteSpace(p.Name) ? p.PodName : p.Name,
                            Component = p.Component,
                            Version = p.Version,
                            PartOf = p.PartOf,
                            PodStatus = p.PodStatus
                        })
                        .DistinctBy(p => p.Name)
                        .ToList();

                    using (var memoryStream = new MemoryStream())
                    {
                        using (var streamWriter = new StreamWriter(memoryStream))
                        {
                            streamWriter.WriteLine($"\"Name\",\"Component\",\"Version\",\"PartOf\",\"PodStatus\"");
                            podList.ForEach(p => streamWriter.WriteLine($"\"{p.Name}\",\"{p.Component}\",\"{p.Version}\",\"{p.PartOf}\",\"{p.PodStatus}\""));
                        }
                        bytesInStream = memoryStream.ToArray();
                    }

                    return bytesInStream;
                }
            }
            catch
            {
                return null;
            }
        }

        private class PodMeta
        {
            public string Name { get; set; }
            public string Component { get; set; }
            public string Version { get; set; }
            public string PartOf { get; set; }
            public string PodStatus { get; set; }
        }
    }
}
