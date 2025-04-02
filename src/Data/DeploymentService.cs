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
    public class DeploymentService(ILogger<DeploymentService> logger, IKubernetes kubernetesClient, IMemoryCache memoryCache, IHttpContextAccessor httpContextAccessor)
    {
        private readonly ILogger<DeploymentService> _logger = logger;
        private readonly IKubernetes _kubernetesClient = kubernetesClient;

        private readonly Counter _restartedDeployments = Metrics.CreateCounter(
            "kube_status_deployment_restarted_total",
            "Number of restarts per deployment.",
            new CounterConfiguration
            {
                LabelNames = ["User", "Namespace", "Deployment"]
            });

        private readonly Counter _restartedDeploymentsAll = Metrics.CreateCounter(
            "kube_status_deployment_all_restarted_total",
            "Number of namespace restarts.",
            new CounterConfiguration
            {
                LabelNames = ["User", "Namespace"]
            });

        private readonly Counter _scaledDeployments = Metrics.CreateCounter(
            "kube_status_deployment_scaled_total",
            "Number of scaling events per deployment.",
            new CounterConfiguration
            {
                LabelNames = ["User", "Namespace", "Deployment"]
            });

        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

        public IMemoryCache MemoryCache { get; } = memoryCache;

        public Task<V1DeploymentList?> GetAllNamespacedDeploymentsAsync(string k8sNamespace = "default")
        {
            return MemoryCache.GetOrCreateAsync($"{k8sNamespace}_deployments", async e =>
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

                return await _kubernetesClient.AppsV1.ListNamespacedDeploymentAsync(k8sNamespace);
            });
        }

        public async Task<bool> RestartDeploymentAsync(string name, string k8sNamespace = "default")
        {
            try
            {
                V1Deployment deployment = await _kubernetesClient.AppsV1.ReadNamespacedDeploymentAsync(name, k8sNamespace);
                JsonDocument old = JsonSerializer.SerializeToDocument(deployment, _jsonSerializerOptions);
                var restart = new Dictionary<string, string>
                {
                    ["kubectl.kubernetes.io/restartedAt"] = $"{DateTimeOffset.Now.UtcDateTime:s}Z"
                };

                deployment.Spec.Template.Metadata.Annotations = restart;

                JsonDocument expected = JsonSerializer.SerializeToDocument(deployment);

                JsonPatch patch = old.CreatePatch(expected);
                _ = await _kubernetesClient.AppsV1.PatchNamespacedDeploymentAsync(new V1Patch(patch, V1Patch.PatchType.JsonPatch), name, k8sNamespace);

                _restartedDeployments.WithLabels(_httpContextAccessor.GetUserIdentityName(), k8sNamespace, name).Inc();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("{ex}", ex.Message);
                return false;
            }
        }

        public async Task<bool> RestartNamespacedDeploymentAsync(string k8sNamespace = "default")
        {
            try
            {
                V1NamespaceList namespaces = await _kubernetesClient.CoreV1.ListNamespaceAsync();
                if (!namespaces.Items.Any(n => n.Metadata.Name.Equals(k8sNamespace, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                V1DeploymentList deployments = await _kubernetesClient.AppsV1.ListNamespacedDeploymentAsync(k8sNamespace);

                if (deployments?.Items != null && deployments.Items.Any())
                {
                    foreach (V1Deployment? deployment in deployments.Items)
                    {
                        _ = await RestartDeploymentAsync(deployment.Metadata.Name, k8sNamespace);
                    }
                }

                _restartedDeploymentsAll.WithLabels(_httpContextAccessor.GetUserIdentityName(), k8sNamespace).Inc();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("{ex}", ex.Message);
                return false;
            }
        }

        public async Task<bool> ScaleDeploymentAsync(string name, int replicas, string k8sNamespace = "default")
        {
            try
            {
                V1NamespaceList namespaces = await _kubernetesClient.CoreV1.ListNamespaceAsync();
                if (!namespaces.Items.Any(n => n.Metadata.Name.Equals(k8sNamespace, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                string patchStr = $"{{\"spec\": {{\"replicas\": {replicas}}}}}";

                _ = await _kubernetesClient.AppsV1.PatchNamespacedDeploymentAsync(new V1Patch(patchStr, V1Patch.PatchType.MergePatch), name, k8sNamespace);

                _scaledDeployments.WithLabels(_httpContextAccessor.GetUserIdentityName(), k8sNamespace, name).Inc();

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
