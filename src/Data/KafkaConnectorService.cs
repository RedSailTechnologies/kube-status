using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeStatus.Models;

namespace KubeStatus.Data
{
    public class KafkaConnectorService
    {
        static readonly HttpClient client = new HttpClient();

        public async Task<IEnumerable<KafkaConnector>> GetAllKafkaConnectorsAsync()
        {
            return await GetKafkaConnectors();
        }

        public async Task<IEnumerable<KafkaConnector>> GetKafkaConnectorsByTaskStateAsync(string taskState = "failed")
        {
            return await Task.FromResult(GetKafkaConnectors().Result.Where(c => c.TaskState.Equals(taskState, System.StringComparison.OrdinalIgnoreCase)));
        }

        public async Task<IEnumerable<KafkaConnector>> RestartAllFailedKafkaConnectorsAsync()
        {
            var kafkaConnectors = await GetKafkaConnectors();
            var failedKafkaConnectors = kafkaConnectors.Where(c => c.TaskState.Equals("failed", System.StringComparison.OrdinalIgnoreCase));
            foreach (var failedKafkaConnector in failedKafkaConnectors)
            {
                var patchStr = @"
{
    ""metadata"": {
        ""annotations"": {
            ""strimzi.io/restart-task"": ""0""
        }
    }
}";

                var client = Helper.GetKubernetesClient();
                await client.PatchNamespacedCustomObjectAsync(new V1Patch(patchStr, V1Patch.PatchType.MergePatch), Helper.StrimziGroup(), Helper.StrimziConnectorVersion(), failedKafkaConnector.Namespace, Helper.StrimziConnectorPlural(), failedKafkaConnector.Name);
            }

            return await Task.FromResult(failedKafkaConnectors);
        }

        private async Task<IEnumerable<KafkaConnector>> GetKafkaConnectors()
        {
            var kafkaConnectors = new List<KafkaConnector>();

            var client = Helper.GetKubernetesClient();

            var response = await client.ListClusterCustomObjectAsync(Helper.StrimziGroup(), Helper.StrimziConnectorVersion(), Helper.StrimziConnectorPlural());
            var jsonString = JsonSerializer.Serialize<object>(response);
            JsonNode jsonNode = JsonNode.Parse(jsonString)!;
            JsonNode itemsNode = jsonNode!["items"]!;

            foreach (var item in itemsNode.AsArray())
            {
                var connectorName = item!["metadata"]!["name"]!.ToString();
                var connectorNamespace = item!["metadata"]!["namespace"]!.ToString();

                var connectorState = string.Empty;
                var taskState = string.Empty;
                var taskTrace = string.Empty;
                var lastTransitionTime = string.Empty;
                var topics = new List<string>();

                if (item!["status"].AsObject().Any(t => t.Key.Equals("connectorStatus")))
                {
                    connectorState = item!["status"]!["connectorStatus"]!["connector"]!["state"]!.ToString();

                    var taskStateObj = item!["status"]!["connectorStatus"]!["tasks"]![0]["state"];
                    if (taskStateObj != null)
                    {
                        taskState = taskStateObj.ToString();
                    }

                    var taskTraceObj = item!["status"]!["connectorStatus"]!["tasks"]![0]!["trace"];
                    if (taskTraceObj != null)
                    {
                        taskTrace = taskTraceObj.ToString();
                    }
                }
                else
                {
                    connectorState = item!["status"]!["conditions"]![0]!["type"].ToString();

                    var taskTraceObj = item!["status"]!["conditions"]![0]!["message"];
                    if (taskTraceObj != null)
                    {
                        taskTrace = taskTraceObj.ToString();
                    }
                }

                if (item!["status"]!["conditions"]![0].AsObject().Any(t => t.Key.Contains("lastTransitionTime")))
                {
                    lastTransitionTime = $"{item!["status"]!["conditions"]![0]!["lastTransitionTime"]!} (UTC)";
                }

                var topicsObj = item!["status"]!["topics"];
                if (topicsObj != null)
                {
                    topics = topicsObj.AsArray().Select(c => c.ToString()).ToList();
                }

                kafkaConnectors.Add(new KafkaConnector
                {
                    Name = connectorName,
                    Namespace = connectorNamespace,
                    LastTransitionTime = lastTransitionTime,
                    ConnectorState = connectorState,
                    TaskState = taskState,
                    TaskTrace = taskTrace,
                    Topics = topics
                });
            }

            return kafkaConnectors;
        }

        public async Task<string> GetConnectorsStatusAsync(bool expandStatus = true, bool expandInfo = true)
        {
            var host = Helper.StrimziConnectClusterServiceHost();
            if (string.IsNullOrWhiteSpace(host))
            {
                return "{}";
            }
            else
            {
                var urlParam = string.Empty;
                if (expandStatus && expandInfo)
                {
                    urlParam = "?expand=status&expand=info";
                }
                else if (expandStatus)
                {
                    urlParam = "?expand=status";
                }
                else if (expandInfo)
                {
                    urlParam = "?expand=info";
                }

                var uri = $"{host.TrimEnd(new char[] { '\\', '/' })}/connectors{urlParam}";

                return await client.GetStringAsync(uri);
            }
        }
    }
}

