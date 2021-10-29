using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeStatus.Models;
using Newtonsoft.Json.Linq;

namespace KubeStatus.Data
{
    public class KafkaConnectorService
    {
        public async Task<IEnumerable<KafkaConnector>> GetAllKafkaConnectorsAsync()
        {
            return await Task.FromResult(GetKafkaConnectors());
        }

        public async Task<IEnumerable<KafkaConnector>> GetKafkaConnectorsByTaskStateAsync(string taskState = "failed")
        {
            return await Task.FromResult(GetKafkaConnectors().Where(c => c.TaskState.Equals(taskState, System.StringComparison.OrdinalIgnoreCase)));
        }

        public async Task<IEnumerable<KafkaConnector>> RestartAllFailedKafkaConnectorsAsync()
        {
            var failedKafkaConnectors = GetKafkaConnectors().Where(c => c.TaskState.Equals("failed", System.StringComparison.OrdinalIgnoreCase));
            foreach (var failedKafkaConnector in failedKafkaConnectors)
            {
                var patchStr = @"
{
    ""metadata"": {
        ""annotations"": {
            ""strimzi.io/restart"": ""true""
        }
    }
}";

                var client = Helper.GetKubernetesClient();
                client.PatchNamespacedCustomObject(new V1Patch(patchStr, V1Patch.PatchType.MergePatch), Helper.StrimziGroup(), Helper.StrimziConnectorVersion(), failedKafkaConnector.Namespace, Helper.StrimziConnectorPlural(), failedKafkaConnector.Name);
            }

            return await Task.FromResult(failedKafkaConnectors);
        }

        private IEnumerable<KafkaConnector> GetKafkaConnectors()
        {
            var kafkaConnectors = new List<KafkaConnector>();

            var client = Helper.GetKubernetesClient();

            var clusterCustomObjects = ((JObject)client.ListClusterCustomObject(Helper.StrimziGroup(), Helper.StrimziConnectorVersion(), Helper.StrimziConnectorPlural())).SelectToken("items").Children();

            foreach (var clusterCustomObject in clusterCustomObjects)
            {
                var connectorName = clusterCustomObject.SelectToken("metadata.name").ToString();
                var connectorNamespace = clusterCustomObject.SelectToken("metadata.namespace").ToString();

                var connectorState = string.Empty;
                var taskState = string.Empty;
                var taskTrace = string.Empty;
                var lastTransitionTime = string.Empty;

                if (clusterCustomObject.SelectToken("status").Children().Any(t => t.Path.Contains("status.connectorStatus")))
                {
                    connectorState = clusterCustomObject.SelectToken("status.connectorStatus.connector.state").ToString();

                    var taskStateObj = clusterCustomObject.SelectToken("status.connectorStatus.tasks[0].state", false);
                    if (taskStateObj != null)
                    {
                        taskState = taskStateObj.ToString();
                    }

                    var taskTraceObj = clusterCustomObject.SelectToken("status.connectorStatus.tasks[0].trace", false);
                    if (taskTraceObj != null)
                    {
                        taskTrace = taskTraceObj.ToString();
                    }
                }
                else
                {
                    connectorState = clusterCustomObject.SelectToken("status.conditions[0].type").ToString();

                    var taskTraceObj = clusterCustomObject.SelectToken("status.conditions[0].message", false);
                    if (taskTraceObj != null)
                    {
                        taskTrace = taskTraceObj.ToString();
                    }
                }

                if (clusterCustomObject.SelectToken("status.conditions[0]").Children().Any(t => t.Path.Contains("status.conditions[0].lastTransitionTime")))
                {
                    lastTransitionTime = $"{clusterCustomObject.SelectToken("status.conditions[0].lastTransitionTime")} (UTC)";
                }

                kafkaConnectors.Add(new KafkaConnector
                {
                    Name = connectorName,
                    Namespace = connectorNamespace,
                    LastTransitionTime = lastTransitionTime,
                    ConnectorState = connectorState,
                    TaskState = taskState,
                    TaskTrace = taskTrace
                });
            }

            return kafkaConnectors;
        }
    }
}
