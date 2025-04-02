using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeStatus.Models;
using Microsoft.Extensions.Logging;

namespace KubeStatus.Data
{
    public class TorEnterpriseService(ILogger<TorEnterpriseService> logger, IKubernetes kubernetesClient)
    {
        private readonly ILogger<TorEnterpriseService> _logger = logger;
        private readonly IKubernetes _kubernetesClient = kubernetesClient;
        private static readonly JsonSerializerOptions s_jsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public async Task<TorEnterprise?> TriggerTorEnterpriseProcessingAsync(string name, string k8sNamespace = "default")
        {
            TorEnterprise? torEnterprise = await GetTorEnterpriseAsync(name, k8sNamespace);
            if (torEnterprise != null)
            {

                string patchStr = @"
{
    ""metadata"": {
        ""annotations"": {
            ""redsail.tor/trigger-processing"": ""true""
        }
    }
}";

                _ = await _kubernetesClient.CustomObjects.PatchNamespacedCustomObjectAsync(new V1Patch(patchStr, V1Patch.PatchType.MergePatch), Helper.TorGroup(), Helper.TorEnterpriseVersion(), k8sNamespace, Helper.TorEnterprisePlural(), name);

                return await Task.FromResult(torEnterprise);
            }

            return null;
        }

        public async Task<IEnumerable<TorEnterprise>> GetAllTorEnterprisesAsync(string? k8sNamespace = null)
        {
            var torEnterprises = new List<TorEnterprise>();

            object response = await _kubernetesClient.CustomObjects.ListClusterCustomObjectAsync(Helper.TorGroup(), Helper.TorEnterpriseVersion(), Helper.TorEnterprisePlural());
            string jsonString = JsonSerializer.Serialize(response);
            _logger.LogDebug("Response: {jsonString}", jsonString);

            JsonNode jsonNode = JsonNode.Parse(jsonString)!;
            JsonNode itemsNode = jsonNode!["items"]!;

            foreach (JsonNode? item in itemsNode.AsArray())
            {
                if (item != null)
                {
                    if (string.IsNullOrWhiteSpace(k8sNamespace) || k8sNamespace == item!["metadata"]!["namespace"]!.ToString())
                    {
                        string enterpriseName = item!["metadata"]!["name"]!.ToString();
                        string enterpriseNamespace = item!["metadata"]!["namespace"]!.ToString();

                        _logger.LogDebug("Spec String: {specString}", item!["spec"]);
                        EnterpriseSpec? spec = JsonSerializer.Deserialize<EnterpriseSpec>(item!["spec"], s_jsonSerializerOptions);

                        _logger.LogDebug("Status String: {statusString}", item!["status"]);
                        EnterpriseStatus? status = JsonSerializer.Deserialize<EnterpriseStatus>(item!["status"], s_jsonSerializerOptions);

                        torEnterprises.Add(new TorEnterprise
                        {
                            Name = enterpriseName,
                            K8sNamespace = enterpriseNamespace,
                            Spec = spec ?? new EnterpriseSpec(),
                            Status = status ?? new EnterpriseStatus()
                        });
                    }
                }
            }

            return torEnterprises;
        }

        public async Task<TorEnterprise?> GetTorEnterpriseAsync(string name, string k8sNamespace = "default")
        {
            object response = await _kubernetesClient.CustomObjects.GetNamespacedCustomObjectAsync(Helper.TorGroup(), Helper.TorEnterpriseVersion(), k8sNamespace, Helper.TorEnterprisePlural(), name);
            string jsonString = JsonSerializer.Serialize(response);
            _logger.LogDebug("Response: {jsonString}", jsonString);

            JsonNode jsonNode = JsonNode.Parse(jsonString)!;

            string enterpriseName = jsonNode!["metadata"]!["name"]!.ToString();
            string enterpriseNamespace = jsonNode!["metadata"]!["namespace"]!.ToString();

            _logger.LogDebug("Spec String: {specString}", jsonNode!["spec"]);
            EnterpriseSpec? spec = JsonSerializer.Deserialize<EnterpriseSpec>(jsonNode!["spec"], s_jsonSerializerOptions);

            _logger.LogDebug("Status String: {statusString}", jsonNode!["status"]);
            EnterpriseStatus? status = JsonSerializer.Deserialize<EnterpriseStatus>(jsonNode!["status"], s_jsonSerializerOptions);

            var torEnterprise = new TorEnterprise
            {
                Name = enterpriseName,
                K8sNamespace = enterpriseNamespace,
                Spec = spec ?? new EnterpriseSpec(),
                Status = status ?? new EnterpriseStatus()
            };

            return torEnterprise;
        }

        public async Task DeleteTorEnterpriseAsync(string name, string k8sNamespace = "default")
        {
            _ = await _kubernetesClient.CustomObjects.DeleteNamespacedCustomObjectAsync(Helper.TorGroup(), Helper.TorEnterpriseVersion(), k8sNamespace, Helper.TorEnterprisePlural(), name).ConfigureAwait(false);
        }
    }
}

