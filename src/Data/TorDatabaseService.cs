using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;

using KubeStatus.Models;

namespace KubeStatus.Data
{
    public class TorDatabaseService(ILogger<TorDatabaseService> logger, IKubernetes kubernetesClient)
    {
        private readonly ILogger<TorDatabaseService> _logger = logger;
        private readonly IKubernetes kubernetesClient = kubernetesClient;
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public async Task<IEnumerable<TorDatabase>> GetAllTorDatabasesAsync(string? k8sNamespace = null)
        {
            var torDatabases = new List<TorDatabase>();

            var response = await kubernetesClient.CustomObjects.ListClusterCustomObjectAsync(Helper.TorGroup(), Helper.TorDatabaseVersion(), Helper.TorDatabasePlural());
            var jsonString = JsonSerializer.Serialize<object>(response);
            _logger.LogDebug("Response: {jsonString}", jsonString);

            JsonNode jsonNode = JsonNode.Parse(jsonString)!;
            JsonNode itemsNode = jsonNode!["items"]!;

            foreach (var item in itemsNode.AsArray())
            {
                if (item != null)
                {
                    if (k8sNamespace == null || k8sNamespace == item!["metadata"]!["namespace"]!.ToString())
                    {
                        var databaseName = item!["metadata"]!["name"]!.ToString();
                        var databaseNamespace = item!["metadata"]!["namespace"]!.ToString();

                        _logger.LogDebug("Spec String: {specString}", item!["spec"]);
                        var spec = JsonSerializer.Deserialize<DatabaseSpec>(item!["spec"], _jsonSerializerOptions);

                        _logger.LogDebug("Status String: {statusString}", item!["status"]);
                        var status = JsonSerializer.Deserialize<DatabaseStatus>(item!["status"], _jsonSerializerOptions);

                        torDatabases.Add(new TorDatabase
                        {
                            Name = databaseName,
                            K8sNamespace = databaseNamespace,
                            Spec = spec ?? new DatabaseSpec(),
                            Status = status ?? new DatabaseStatus()
                        });
                    }
                }
            }

            return torDatabases;
        }

        public async Task<TorDatabase?> GetTorDatabaseAsync(string name, string k8sNamespace = "default")
        {
            var response = await kubernetesClient.CustomObjects.GetNamespacedCustomObjectAsync(Helper.TorGroup(), Helper.TorDatabaseVersion(), k8sNamespace, Helper.TorDatabasePlural(), name);
            var jsonString = JsonSerializer.Serialize(response);
            _logger.LogDebug("Response: {jsonString}", jsonString);

            JsonNode jsonNode = JsonNode.Parse(jsonString)!;

            var databaseName = jsonNode!["metadata"]!["name"]!.ToString();
            var databaseNamespace = jsonNode!["metadata"]!["namespace"]!.ToString();

            _logger.LogDebug("Spec String: {specString}", jsonNode!["spec"]);
            var spec = JsonSerializer.Deserialize<DatabaseSpec>(jsonNode!["spec"], _jsonSerializerOptions);

            _logger.LogDebug("Status String: {statusString}", jsonNode!["status"]);
            var status = JsonSerializer.Deserialize<DatabaseStatus>(jsonNode!["status"], _jsonSerializerOptions);

            var torDatabase = new TorDatabase
            {
                Name = databaseName,
                K8sNamespace = databaseNamespace,
                Spec = spec ?? new DatabaseSpec(),
                Status = status ?? new DatabaseStatus()
            };

            return torDatabase;
        }

        public async Task DeleteTorDatabaseAsync(string name, string k8sNamespace = "default")
        {
            await kubernetesClient.CustomObjects.DeleteNamespacedCustomObjectAsync(Helper.TorGroup(), Helper.TorDatabaseVersion(), k8sNamespace, Helper.TorDatabasePlural(), name).ConfigureAwait(false);
        }
    }
}

