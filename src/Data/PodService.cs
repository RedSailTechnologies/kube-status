using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeStatus.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace KubeStatus.Data
{
    public class PodService(ILogger<PodService> logger, IKubernetes kubernetesClient, IMemoryCache memoryCache)
    {
        private readonly ILogger<PodService> _logger = logger;
        private readonly IKubernetes _kubernetesClient = kubernetesClient;
        private static readonly HttpClient s_httpClient = new();

        public IMemoryCache MemoryCache { get; } = memoryCache;

        public Task<IEnumerable<Pod>?> GetAllNamespacedPodsAsync(string k8sNamespace = "default")
        {
            return MemoryCache.GetOrCreateAsync($"{k8sNamespace}_pods", async e =>
            {
                _ = e.SetOptions(new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromSeconds(10)
                });

                V1NamespaceList namespaces = await _kubernetesClient.CoreV1.ListNamespaceAsync();
                if (namespaces == null || !namespaces.Items.Any(n => n.Metadata.Name.Equals(k8sNamespace, System.StringComparison.OrdinalIgnoreCase)))
                {
                    return null;
                }

                V1PodList list = await _kubernetesClient.CoreV1.ListNamespacedPodAsync(k8sNamespace);
                Corev1EventList events = await _kubernetesClient.CoreV1.ListNamespacedEventAsync(k8sNamespace);

                var pods = new List<Pod>();

                foreach (V1Pod? item in list.Items)
                {
                    List<Corev1Event> podEvents = [];
                    if (item?.Metadata?.Uid != null)
                    {
                        foreach (Corev1Event? podEvent in events.Items.OrderByDescending(i => i.LastTimestamp))
                        {
                            string itemUid = item.Metadata.Uid;
                            string eventUid = podEvent.InvolvedObject.Uid;
                            if (itemUid.Equals(eventUid))
                            {
                                podEvents.Add(podEvent);
                            }
                        }
                    }

                    if (item != null)
                    {
                        pods.Add(new Pod
                        {
                            Name = item.Metadata.Labels.TryGetValue("app.kubernetes.io/name", out string? labelName) ? labelName : "",
                            Instance = item.Metadata.Labels.TryGetValue("app.kubernetes.io/instance", out string? labelInstance) ? labelInstance : "",
                            Version = item.Metadata.Labels.TryGetValue("app.kubernetes.io/version", out string? labelVersion) ? labelVersion : "",
                            Component = item.Metadata.Labels.TryGetValue("app.kubernetes.io/component", out string? labelComponent) ? labelComponent : "",
                            PartOf = item.Metadata.Labels.TryGetValue("app.kubernetes.io/part-of", out string? labelPartOf) ? labelPartOf : "",
                            ManagedBy = item.Metadata.Labels.TryGetValue("app.kubernetes.io/managed-by", out string? labelManagedBy) ? labelManagedBy : "",
                            CreatedBy = item.Metadata.Labels.TryGetValue("app.kubernetes.io/created-by", out string? labelCreatedBy) ? labelCreatedBy : "",
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
                }

                return await Task.FromResult(pods.AsEnumerable());
            });
        }

        private IList<V1ContainerAndStatus> SortContainersAndStatuses(IList<V1ContainerStatus> statuses, IList<V1Container> containers)
        {
            var list = new List<V1ContainerAndStatus>();

            if (containers != null && containers.Count > 0)
            {
                foreach (V1Container? container in containers.OrderBy(c => c.Name))
                {
                    string name = container.Name;
                    V1ContainerStatus? status = statuses?.FirstOrDefault(s => s.Name.Equals(name));
                    if (status == null)
                    {
                        status = new V1ContainerStatus();
                    }
                    list.Add(new V1ContainerAndStatus { ContainerStatus = status, Container = container });
                }
            }
            else if (statuses != null && statuses.Count > 0)
            {
                foreach (V1ContainerStatus? status in statuses.OrderBy(s => s.Name))
                {
                    list.Add(new V1ContainerAndStatus { ContainerStatus = status });
                }
            }

            return list;
        }

        public async Task<List<EnvironmentVariable>> GetContainerEnvironmentVariablesAsync(string k8sNamespace, string podName, string containerName, bool showSecrets)
        {
            V1Pod pod = await _kubernetesClient.CoreV1.ReadNamespacedPodAsync(podName, k8sNamespace);
            List<V1Container> containers = [.. pod.Spec.Containers, .. pod.Spec.InitContainers];
            V1Container? container = containers.FirstOrDefault(c => c.Name.Equals(containerName));
            if (container != null)
            {
                return await GetEnvironmentVariablesAndValues(container, k8sNamespace, showSecrets);
            }
            else
            {
                return [];
            }
        }

        private async Task<List<EnvironmentVariable>> GetEnvironmentVariablesAndValues(V1Container container, string k8sNamespace, bool showSecrets)
        {
            List<EnvironmentVariable> envVars = [];

            if (container.Env != null && container.Env.Any())
            {
                foreach (V1EnvVar? envVar in container.Env)
                {
                    if (envVar.ValueFrom == null)
                    {
                        envVars.Add(new EnvironmentVariable() { Key = envVar.Name, Value = envVar.Value, Type = "Simple" });
                    }
                    else
                    {
                        if (envVar.ValueFrom.SecretKeyRef != null)
                        {
                            if (showSecrets)
                            {
                                V1Secret secret = await _kubernetesClient.CoreV1.ReadNamespacedSecretAsync(envVar.ValueFrom.SecretKeyRef.Name, k8sNamespace);
                                envVars.Add(new EnvironmentVariable() { Key = envVar.Name, Value = System.Text.Encoding.UTF8.GetString(secret.Data.FirstOrDefault(d => d.Key.Equals(envVar.ValueFrom.SecretKeyRef.Key)).Value), Type = "Secret" });
                            }
                            else
                            {
                                envVars.Add(new EnvironmentVariable() { Key = envVar.Name, Value = "***", Type = "Secret" });
                            }
                        }
                        else if (envVar.ValueFrom.ConfigMapKeyRef != null)
                        {
                            V1ConfigMap configMap = await _kubernetesClient.CoreV1.ReadNamespacedConfigMapAsync(envVar.ValueFrom.ConfigMapKeyRef.Name, k8sNamespace);
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
                foreach (V1EnvFromSource? envVarRef in container.EnvFrom)
                {
                    if (envVarRef.ConfigMapRef != null)
                    {
                        V1ConfigMap configMap = await _kubernetesClient.CoreV1.ReadNamespacedConfigMapAsync(envVarRef.ConfigMapRef.Name, k8sNamespace);
                        if (configMap.Data != null && configMap.Data.Any())
                        {
                            foreach (KeyValuePair<string, string> val in configMap.Data)
                            {
                                envVars.Add(new EnvironmentVariable() { Key = val.Key, Value = val.Value, Type = "ConfigMap" });
                            }
                        }
                    }

                    if (envVarRef.SecretRef != null)
                    {
                        V1Secret secret = await _kubernetesClient.CoreV1.ReadNamespacedSecretAsync(envVarRef.SecretRef.Name, k8sNamespace);
                        if (secret.Data != null && secret.Data.Any())
                        {
                            foreach (KeyValuePair<string, byte[]> val in secret.Data)
                            {
                                if (showSecrets)
                                {
                                    envVars.Add(new EnvironmentVariable() { Key = val.Key, Value = System.Text.Encoding.UTF8.GetString(val.Value), Type = "Secret" });
                                }
                                else
                                {
                                    envVars.Add(new EnvironmentVariable() { Key = val.Key, Value = "***", Type = "Secret" });
                                }
                            }
                        }
                    }
                }
            }

            return envVars;
        }

        public async Task<byte[]?> GetAllNamespacedPodsFileAsync(string k8sNamespace = "default")
        {
            try
            {
                IEnumerable<Pod>? pods = await GetAllNamespacedPodsAsync(k8sNamespace);
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
                        .DistinctBy(p => p.Name + ":" + p.Component)
                        .OrderBy(p => p.Name)
                        .ThenBy(p => p.Component)
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
            catch (Exception ex)
            {
                _logger.LogError("{ex}", ex.Message);
                return null;
            }
        }

        public async Task<byte[]?> GetContainerMetricsAsync(string k8sNamespace, string podName, int port)
        {
            try
            {
                V1Pod pod = await _kubernetesClient.CoreV1.ReadNamespacedPodAsync(podName, k8sNamespace);
                string podIP = pod.Status.PodIP;

                if (Helper.IsPrivateIP(podIP) && Helper.IsValidPort(port))
                {
                    string uri = $"http://{podIP}:{port}/{Helper.MetricsRoute()}";
                    return await s_httpClient.GetByteArrayAsync(uri);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("{ex}", ex.Message);
                return null;
            }
        }

        public async Task<bool> PodExistsAsync(string k8sNamespace, string podName)
        {
            try
            {
                V1Pod pod = await _kubernetesClient.CoreV1.ReadNamespacedPodAsync(podName, k8sNamespace);
                if (pod != null)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError("{ex}", ex.Message);
                return false;
            }
        }

        private class PodMeta
        {
            public string? Name { get; set; }
            public string? Component { get; set; }
            public string? Version { get; set; }
            public string? PartOf { get; set; }
            public string? PodStatus { get; set; }
        }
    }
}
