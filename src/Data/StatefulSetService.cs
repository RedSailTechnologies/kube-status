using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Json.Patch;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace KubeStatus.Data
{
    public class StatefulSetService(ILogger<StatefulSetService> logger, IKubernetes kubernetesClient, IMemoryCache memoryCache, IHttpContextAccessor httpContextAccessor)
    {
        private readonly ILogger<StatefulSetService> _logger = logger;
        private readonly IKubernetes _kubernetesClient = kubernetesClient;

        private readonly Counter _restartedStatefulSets = Metrics.CreateCounter(
            "kube_status_statefulset_restarted_total",
            "Number of restarts per statefulset.",
            new CounterConfiguration
            {
                LabelNames = ["User", "Namespace", "StatefulSet"]
            });

        private readonly Counter _restartedStatefulSetsAll = Metrics.CreateCounter(
            "kube_status_statefulset_all_restarted_total",
            "Number of namespace restarts.",
            new CounterConfiguration
            {
                LabelNames = ["User", "Namespace"]
            });

        private readonly Counter _scaledStatefulSets = Metrics.CreateCounter(
            "kube_status_statefulset_scaled_total",
            "Number of scaling events per statefulset.",
            new CounterConfiguration
            {
                LabelNames = ["User", "Namespace", "StatefulSet"]
            });

        private static readonly JsonSerializerOptions s_jsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        public IMemoryCache MemoryCache { get; } = memoryCache;

        public Task<V1StatefulSetList?> GetAllNamespacedStatefulSetsAsync(string k8sNamespace = "default")
        {
            return MemoryCache.GetOrCreateAsync($"{k8sNamespace}_statefulset", async e =>
            {
                _ = e.SetOptions(new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromSeconds(10)
                });

                V1NamespaceList namespaces = await _kubernetesClient.CoreV1.ListNamespaceAsync();
                if (namespaces == null || !namespaces.Items.Any(n => n.Metadata.Name.Equals(k8sNamespace, StringComparison.OrdinalIgnoreCase)))
                {
                    return null;
                }

                return await _kubernetesClient.AppsV1.ListNamespacedStatefulSetAsync(k8sNamespace);
            });
        }

        public async Task<bool> RestartStatefulSetAsync(string name, string k8sNamespace = "default")
        {
            try
            {
                V1StatefulSet StatefulSet = await _kubernetesClient.AppsV1.ReadNamespacedStatefulSetAsync(name, k8sNamespace);
                JsonDocument old = JsonSerializer.SerializeToDocument(StatefulSet, s_jsonSerializerOptions);
                var restart = new Dictionary<string, string>
                {
                    ["kubectl.kubernetes.io/restartedAt"] = $"{DateTimeOffset.Now.UtcDateTime:s}Z"
                };

                StatefulSet.Spec.Template.Metadata.Annotations = restart;

                JsonDocument expected = JsonSerializer.SerializeToDocument(StatefulSet);

                JsonPatch patch = old.CreatePatch(expected);
                _ = await _kubernetesClient.AppsV1.PatchNamespacedStatefulSetAsync(new V1Patch(patch, V1Patch.PatchType.JsonPatch), name, k8sNamespace);

                _restartedStatefulSets.WithLabels(_httpContextAccessor.GetUserIdentityName(), k8sNamespace, name).Inc();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("{ex}", ex.Message);
                return false;
            }
        }

        public async Task<bool> RestartNamespacedStatefulSetAsync(string k8sNamespace = "default")
        {
            try
            {
                V1NamespaceList namespaces = await _kubernetesClient.CoreV1.ListNamespaceAsync();
                if (!namespaces.Items.Any(n => n.Metadata.Name.Equals(k8sNamespace, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                V1StatefulSetList StatefulSets = await _kubernetesClient.AppsV1.ListNamespacedStatefulSetAsync(k8sNamespace);

                if (StatefulSets?.Items != null && StatefulSets.Items.Any())
                {
                    foreach (V1StatefulSet? StatefulSet in StatefulSets.Items)
                    {
                        _ = await RestartStatefulSetAsync(StatefulSet.Metadata.Name, k8sNamespace);
                    }
                }

                _restartedStatefulSetsAll.WithLabels(_httpContextAccessor.GetUserIdentityName(), k8sNamespace).Inc();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("{ex}", ex.Message);
                return false;
            }
        }

        public async Task<bool> ScaleStatefulSetAsync(string name, int replicas, string k8sNamespace = "default")
        {
            try
            {
                V1NamespaceList namespaces = await _kubernetesClient.CoreV1.ListNamespaceAsync();
                if (!namespaces.Items.Any(n => n.Metadata.Name.Equals(k8sNamespace, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                string patchStr = $"{{\"spec\": {{\"replicas\": {replicas}}}}}";

                _ = await _kubernetesClient.AppsV1.PatchNamespacedStatefulSetAsync(new V1Patch(patchStr, V1Patch.PatchType.MergePatch), name, k8sNamespace);

                _scaledStatefulSets.WithLabels(_httpContextAccessor.GetUserIdentityName(), k8sNamespace, name).Inc();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("{ex}", ex.Message);
                return false;
            }
        }
    }
}
