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
    public class KafkaConnectorService(IKubernetes kubernetesClient)
    {
        private readonly IKubernetes _kubernetesClient = kubernetesClient;
        private static readonly HttpClient s_httpClient = new();

        public async Task<IEnumerable<KafkaConnector>> GetAllKafkaConnectorsAsync()
        {
            return await GetKafkaConnectors();
        }

        public async Task<IEnumerable<KafkaConnector>> GetKafkaConnectorsByTaskStateAsync(string taskState = "failed")
        {
            return await Task.FromResult(GetKafkaConnectors().Result.Where(c => c.TaskState != null && c.TaskState.Equals(taskState, System.StringComparison.OrdinalIgnoreCase)));
        }

        public async Task<IEnumerable<KafkaConnector>> RestartAllFailedKafkaConnectorsAsync()
        {
            IEnumerable<KafkaConnector> kafkaConnectors = await GetKafkaConnectors();
            IEnumerable<KafkaConnector> failedKafkaConnectors = kafkaConnectors.Where(c => c.TaskState != null && c.TaskState.Equals("failed", System.StringComparison.OrdinalIgnoreCase));
            foreach (KafkaConnector? failedKafkaConnector in failedKafkaConnectors)
            {
                string patchStr = @"
{
    ""metadata"": {
        ""annotations"": {
            ""strimzi.io/restart-task"": ""0""
        }
    }
}";

                _ = await _kubernetesClient.CustomObjects.PatchNamespacedCustomObjectAsync(new V1Patch(patchStr, V1Patch.PatchType.MergePatch), Helper.StrimziGroup(), Helper.StrimziConnectorVersion(), failedKafkaConnector.Namespace, Helper.StrimziConnectorPlural(), failedKafkaConnector.Name);
            }

            return await Task.FromResult(failedKafkaConnectors);
        }

        public async Task<KafkaConnector> RestartKafkaConnectorAsync(string connectorName, string k8sNamespace = "default")
        {
            KafkaConnector kafkaConnector = await GetKafkaConnector(connectorName, k8sNamespace);

            string patchStr = @"
{
    ""metadata"": {
        ""annotations"": {
            ""strimzi.io/restart-task"": ""0""
        }
    }
}";

            _ = await _kubernetesClient.CustomObjects.PatchNamespacedCustomObjectAsync(new V1Patch(patchStr, V1Patch.PatchType.MergePatch), Helper.StrimziGroup(), Helper.StrimziConnectorVersion(), kafkaConnector.Namespace, Helper.StrimziConnectorPlural(), kafkaConnector.Name);

            return await Task.FromResult(kafkaConnector);
        }

        private async Task<IEnumerable<KafkaConnector>> GetKafkaConnectors()
        {
            var kafkaConnectors = new List<KafkaConnector>();

            object response = await _kubernetesClient.CustomObjects.ListClusterCustomObjectAsync(Helper.StrimziGroup(), Helper.StrimziConnectorVersion(), Helper.StrimziConnectorPlural());
            string jsonString = JsonSerializer.Serialize<object>(response);
            JsonNode jsonNode = JsonNode.Parse(jsonString)!;
            JsonNode itemsNode = jsonNode!["items"]!;

            foreach (JsonNode? item in itemsNode.AsArray())
            {
                if (item != null)
                {
                    string connectorName = item!["metadata"]!["name"]!.ToString();
                    string connectorNamespace = item!["metadata"]!["namespace"]!.ToString();

                    string connectorState = string.Empty;
                    string taskState = string.Empty;
                    string taskTrace = string.Empty;
                    string lastTransitionTime = string.Empty;
                    var topics = new List<string>();

                    if (item!["status"]!.AsObject().Any(t => t.Key.Equals("connectorStatus")))
                    {
                        connectorState = item!["status"]!["connectorStatus"]!["connector"]!["state"]!.ToString();

                        JsonNode? taskStateObj = item!["status"]!["connectorStatus"]!["tasks"]![0]!["state"];
                        if (taskStateObj != null)
                        {
                            taskState = taskStateObj.ToString();
                        }

                        JsonNode? taskTraceObj = item!["status"]!["connectorStatus"]!["tasks"]![0]!["trace"];
                        if (taskTraceObj != null)
                        {
                            taskTrace = taskTraceObj.ToString();
                        }
                    }
                    else
                    {
                        connectorState = item!["status"]!["conditions"]![0]!["type"]!.ToString();

                        JsonNode? taskTraceObj = item!["status"]!["conditions"]![0]!["message"];
                        if (taskTraceObj != null)
                        {
                            taskTrace = taskTraceObj.ToString();
                        }
                    }

                    if (item!["status"]!["conditions"]![0]!.AsObject().Any(t => t.Key.Contains("lastTransitionTime")))
                    {
                        lastTransitionTime = $"{item!["status"]!["conditions"]![0]!["lastTransitionTime"]!} (UTC)";
                    }

                    JsonNode? topicsObj = item!["status"]!["topics"];
                    if (topicsObj != null)
                    {
                        topics = [.. topicsObj.AsArray().Select(c => c!.ToString())];
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
            }

            return kafkaConnectors;
        }

        private async Task<KafkaConnector> GetKafkaConnector(string name, string k8sNamespace = "default")
        {
            object response = await _kubernetesClient.CustomObjects.GetNamespacedCustomObjectAsync(Helper.StrimziGroup(), Helper.StrimziConnectorVersion(), k8sNamespace, Helper.StrimziConnectorPlural(), name);
            string jsonString = JsonSerializer.Serialize(response);
            JsonNode? item = JsonNode.Parse(jsonString);

            string connectorName = item!["metadata"]!["name"]!.ToString();
            string connectorNamespace = item!["metadata"]!["namespace"]!.ToString();
            string connectorState = string.Empty;
            string taskState = string.Empty;
            string taskTrace = string.Empty;
            string lastTransitionTime = string.Empty;
            var topics = new List<string>();

            if (item!["status"]!.AsObject().Any(t => t.Key.Equals("connectorStatus")))
            {
                connectorState = item!["status"]!["connectorStatus"]!["connector"]!["state"]!.ToString();

                JsonNode? taskStateObj = item!["status"]!["connectorStatus"]!["tasks"]![0]!["state"];
                if (taskStateObj != null)
                {
                    taskState = taskStateObj.ToString();
                }

                JsonNode? taskTraceObj = item!["status"]!["connectorStatus"]!["tasks"]![0]!["trace"];
                if (taskTraceObj != null)
                {
                    taskTrace = taskTraceObj.ToString();
                }
            }
            else
            {
                connectorState = item!["status"]!["conditions"]![0]!["type"]!.ToString();

                JsonNode? taskTraceObj = item!["status"]!["conditions"]![0]!["message"];
                if (taskTraceObj != null)
                {
                    taskTrace = taskTraceObj.ToString();
                }
            }

            if (item!["status"]!["conditions"]![0]!.AsObject().Any(t => t.Key.Contains("lastTransitionTime")))
            {
                lastTransitionTime = $"{item!["status"]!["conditions"]![0]!["lastTransitionTime"]!} (UTC)";
            }

            JsonNode? topicsObj = item!["status"]!["topics"];
            if (topicsObj != null)
            {
                topics = [.. topicsObj.AsArray().Select(c => c?.ToString())];
            }

            var kafkaConnector = new KafkaConnector
            {
                Name = connectorName,
                Namespace = connectorNamespace,
                LastTransitionTime = lastTransitionTime,
                ConnectorState = connectorState,
                TaskState = taskState,
                TaskTrace = taskTrace,
                Topics = topics
            };

            return kafkaConnector;
        }

        public async Task<string> GetConnectorsStatusAsync(bool expandStatus = true, bool expandInfo = true)
        {
            string host = Helper.StrimziConnectClusterServiceHost();
            if (string.IsNullOrWhiteSpace(host))
            {
                return "{}";
            }
            else
            {
                string urlParam = string.Empty;
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

                string uri = $"{host.TrimEnd(['\\', '/'])}/connectors{urlParam}";

                return await s_httpClient.GetStringAsync(uri);
            }
        }
    }
}

