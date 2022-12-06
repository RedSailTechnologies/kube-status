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
                    List<Corev1Event> podEvents = new List<Corev1Event>();
                    if (item?.Metadata?.Uid != null)
                    {
                        foreach (var podEvent in events.Items.OrderByDescending(i => i.LastTimestamp))
                        {
                            var itemUid = item.Metadata.Uid;
                            var eventUid = podEvent.InvolvedObject.Uid;
                            if (itemUid.Equals(eventUid))
                            {
                                podEvents.Add(podEvent);
                            }
                        }
                    }

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

        public async Task<List<EnvironmentVariable>> GetContainerEnvironmentVariablesAsync(string k8sNamespace, string podName, string containerName)
        {
            var pod = await kubernetesClient.CoreV1.ReadNamespacedPodAsync(podName, k8sNamespace);
            List<V1Container> containers = new List<V1Container>();
            containers.AddRange(pod.Spec.Containers);
            containers.AddRange(pod.Spec.InitContainers);
            var container = containers.FirstOrDefault(c => c.Name.Equals(containerName));
            if (container != null)
            {
                return await GetEnvironmentVariablesAndValues(container, k8sNamespace);
            }
            else
            {
                return new List<EnvironmentVariable>();
            }
        }

        private async Task<List<EnvironmentVariable>> GetEnvironmentVariablesAndValues(V1Container container, string k8sNamespace)
        {
            List<EnvironmentVariable> envVars = new List<EnvironmentVariable>();

            if (container.Env != null && container.Env.Any())
            {
                foreach (var envVar in container.Env)
                {
                    if (envVar.ValueFrom == null)
                    {
                        envVars.Add(new EnvironmentVariable() { Key = envVar.Name, Value = envVar.Value, Type = "Simple" });
                    }
                    else
                    {
                        if (envVar.ValueFrom.SecretKeyRef != null)
                        {
                            // don't add the secret value for security
                            // var secret = await kubernetesClient.CoreV1.ReadNamespacedSecretAsync(envVar.ValueFrom.SecretKeyRef.Name, k8sNamespace);
                            // envVars.Add(new EnvironmentVariable() { Key = envVar.Name, Value = System.Text.Encoding.UTF8.GetString(secret.Data.FirstOrDefault(d => d.Key.Equals(envVar.ValueFrom.SecretKeyRef.Key)).Value), Type = "Secret" });
                            envVars.Add(new EnvironmentVariable() { Key = envVar.Name, Value = "***", Type = "Secret" });
                        }
                        else if (envVar.ValueFrom.ConfigMapKeyRef != null)
                        {
                            var configMap = await kubernetesClient.CoreV1.ReadNamespacedConfigMapAsync(envVar.ValueFrom.ConfigMapKeyRef.Name, k8sNamespace);
                            envVars.Add(new EnvironmentVariable() { Key = envVar.Name, Value = configMap.Data.FirstOrDefault(d => d.Key.Equals(envVar.ValueFrom.ConfigMapKeyRef.Key)).Value, Type = "ConfigMap" });
                        }
                        else if (envVar.ValueFrom.FieldRef != null)
                        {
                            envVars.Add(new EnvironmentVariable() { Key = envVar.Name, Value = envVar.ValueFrom.ToYaml(), Type = "FieldRef" });
                        }
                        else
                        {
                            envVars.Add(new EnvironmentVariable() { Key = envVar.Name, Value = envVar.ValueFrom.ToYaml(), Type = "ValueFrom" });
                        }
                    }
                }
            }

            if (container.EnvFrom != null && container.EnvFrom.Any())
            {
                foreach (var envVarRef in container.EnvFrom)
                {
                    if (envVarRef.ConfigMapRef != null)
                    {
                        var configMap = await kubernetesClient.CoreV1.ReadNamespacedConfigMapAsync(envVarRef.ConfigMapRef.Name, k8sNamespace);
                        if (configMap.Data != null && configMap.Data.Any())
                        {
                            foreach (var val in configMap.Data)
                            {
                                envVars.Add(new EnvironmentVariable() { Key = val.Key, Value = val.Value, Type = "ConfigMap" });
                            }
                        }
                    }

                    if (envVarRef.SecretRef != null)
                    {
                        var secret = await kubernetesClient.CoreV1.ReadNamespacedSecretAsync(envVarRef.SecretRef.Name, k8sNamespace);
                        if (secret.Data != null && secret.Data.Any())
                        {
                            foreach (var val in secret.Data)
                            {
                                // don't add the secret value for security
                                // envVars.Add(new EnvironmentVariable() { Key = val.Key, Value = System.Text.Encoding.UTF8.GetString(val.Value), Type = "Secret" });
                                envVars.Add(new EnvironmentVariable() { Key = val.Key, Value = "***", Type = "Secret" });
                            }
                        }
                    }
                }
            }

            return envVars;
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
