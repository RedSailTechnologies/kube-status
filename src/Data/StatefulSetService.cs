using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Json.Patch;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Caching.Memory;

namespace KubeStatus.Data
{
    public class StatefulSetService
    {
        private readonly IKubernetes kubernetesClient;

        public IMemoryCache MemoryCache { get; }

        public StatefulSetService(IKubernetes kubernetesClient, IMemoryCache memoryCache)
        {
            this.kubernetesClient = kubernetesClient;
            MemoryCache = memoryCache;
        }

        public Task<V1StatefulSetList> GetAllNamespacedStatefulSetsAsync(string k8sNamespace = "default")
        {
            return MemoryCache.GetOrCreateAsync($"{k8sNamespace}_statefulsets", async e =>
            {
                e.SetOptions(new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromSeconds(10)
                });

                var namespaces = await kubernetesClient.CoreV1.ListNamespaceAsync();
                if (namespaces == null || !namespaces.Items.Any(n => n.Metadata.Name.Equals(k8sNamespace, System.StringComparison.OrdinalIgnoreCase)))
                {
                    return null;
                }

                return await kubernetesClient.AppsV1.ListNamespacedStatefulSetAsync(k8sNamespace);
            });
        }

        public async Task<Boolean> RestartStatefulSetAsync(string name, string k8sNamespace = "default")
        {
            try
            {
                var StatefulSet = await kubernetesClient.AppsV1.ReadNamespacedStatefulSetAsync(name, k8sNamespace);
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
                var old = JsonSerializer.SerializeToDocument(StatefulSet, options);
                var now = DateTimeOffset.Now.ToUnixTimeSeconds();
                var restart = new Dictionary<string, string>
                {
                    ["date"] = now.ToString()
                };

                StatefulSet.Spec.Template.Metadata.Annotations = restart;

                var expected = JsonSerializer.SerializeToDocument(StatefulSet);

                var patch = old.CreatePatch(expected);
                await kubernetesClient.AppsV1.PatchNamespacedStatefulSetAsync(new V1Patch(patch, V1Patch.PatchType.JsonPatch), name, k8sNamespace);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Boolean> RestartNamespacedStatefulSetAsync(string k8sNamespace = "default")
        {
            try
            {
                var namespaces = await kubernetesClient.CoreV1.ListNamespaceAsync();
                if (!namespaces.Items.Any(n => n.Metadata.Name.Equals(k8sNamespace, System.StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                var StatefulSets = await kubernetesClient.AppsV1.ListNamespacedStatefulSetAsync(k8sNamespace);

                if (StatefulSets?.Items != null && StatefulSets.Items.Any())
                {
                    foreach (var StatefulSet in StatefulSets.Items)
                    {
                        await RestartStatefulSetAsync(StatefulSet.Metadata.Name, k8sNamespace);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Boolean> ScaleStatefulSetAsync(string name, int replicas, string k8sNamespace = "default")
        {
            try
            {
                var namespaces = await kubernetesClient.CoreV1.ListNamespaceAsync();
                if (!namespaces.Items.Any(n => n.Metadata.Name.Equals(k8sNamespace, System.StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                var patchStr = $"{{\"spec\": {{\"replicas\": {replicas}}}}}";

                await kubernetesClient.AppsV1.PatchNamespacedStatefulSetAsync(new V1Patch(patchStr, V1Patch.PatchType.MergePatch), name, k8sNamespace);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
