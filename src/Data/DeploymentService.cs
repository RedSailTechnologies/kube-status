using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

using Json.Patch;
using Prometheus;

using k8s;
using k8s.Models;

namespace KubeStatus.Data
{
    public class DeploymentService
    {
        private readonly IKubernetes kubernetesClient;

        private readonly Counter _restartedDeployments = Metrics.CreateCounter(
            "kube_status_deployment_restarted_total",
            "Number of restarts per deployment.",
            new CounterConfiguration
            {
                LabelNames = new[] { "User", "Namespace", "Deployment" }
            });

        private readonly Counter _restartedDeploymentsAll = Metrics.CreateCounter(
            "kube_status_deployment_all_restarted_total",
            "Number of namespace restarts.",
            new CounterConfiguration
            {
                LabelNames = new[] { "User", "Namespace" }
            });

        private readonly Counter _scaledDeployments = Metrics.CreateCounter(
            "kube_status_deployment_scaled_total",
            "Number of scaling events per deployment.",
            new CounterConfiguration
            {
                LabelNames = new[] { "User", "Namespace", "Deployment" }
            });

        private readonly IHttpContextAccessor _httpContextAccessor;

        public IMemoryCache MemoryCache { get; }

        public DeploymentService(IKubernetes kubernetesClient, IMemoryCache memoryCache, IHttpContextAccessor httpContextAccessor)
        {
            this.kubernetesClient = kubernetesClient;
            MemoryCache = memoryCache;
            _httpContextAccessor = httpContextAccessor;
        }

        public Task<V1DeploymentList> GetAllNamespacedDeploymentsAsync(string k8sNamespace = "default")
        {
            return MemoryCache.GetOrCreateAsync($"{k8sNamespace}_deployments", async e =>
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

                return await kubernetesClient.AppsV1.ListNamespacedDeploymentAsync(k8sNamespace);
            });
        }

        public async Task<Boolean> RestartDeploymentAsync(string name, string k8sNamespace = "default")
        {
            try
            {
                var deployment = await kubernetesClient.AppsV1.ReadNamespacedDeploymentAsync(name, k8sNamespace);
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
                var old = JsonSerializer.SerializeToDocument(deployment, options);
                var now = DateTimeOffset.Now.ToUnixTimeSeconds();
                var restart = new Dictionary<string, string>
                {
                    ["date"] = now.ToString()
                };

                deployment.Spec.Template.Metadata.Annotations = restart;

                var expected = JsonSerializer.SerializeToDocument(deployment);

                var patch = old.CreatePatch(expected);
                await kubernetesClient.AppsV1.PatchNamespacedDeploymentAsync(new V1Patch(patch, V1Patch.PatchType.JsonPatch), name, k8sNamespace);

                _restartedDeployments.WithLabels(_httpContextAccessor.GetUserIdentityName(), k8sNamespace, name).Inc();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Boolean> RestartNamespacedDeploymentAsync(string k8sNamespace = "default")
        {
            try
            {
                var namespaces = await kubernetesClient.CoreV1.ListNamespaceAsync();
                if (!namespaces.Items.Any(n => n.Metadata.Name.Equals(k8sNamespace, System.StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                var deployments = await kubernetesClient.AppsV1.ListNamespacedDeploymentAsync(k8sNamespace);

                if (deployments?.Items != null && deployments.Items.Any())
                {
                    foreach (var deployment in deployments.Items)
                    {
                        await RestartDeploymentAsync(deployment.Metadata.Name, k8sNamespace);
                    }
                }

                _restartedDeploymentsAll.WithLabels(_httpContextAccessor.GetUserIdentityName(), k8sNamespace).Inc();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Boolean> ScaleDeploymentAsync(string name, int replicas, string k8sNamespace = "default")
        {
            try
            {
                var namespaces = await kubernetesClient.CoreV1.ListNamespaceAsync();
                if (!namespaces.Items.Any(n => n.Metadata.Name.Equals(k8sNamespace, System.StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                var patchStr = $"{{\"spec\": {{\"replicas\": {replicas}}}}}";

                await kubernetesClient.AppsV1.PatchNamespacedDeploymentAsync(new V1Patch(patchStr, V1Patch.PatchType.MergePatch), name, k8sNamespace);

                _scaledDeployments.WithLabels(_httpContextAccessor.GetUserIdentityName(), k8sNamespace, name).Inc();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
