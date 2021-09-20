using System.Collections.Generic;
using System.Linq;
using k8s;
using k8s.Models;
using KubeStatus.Models;
using Newtonsoft.Json.Linq;

namespace KubeStatus.Repository
{
    public class KafkaConnectorsRepository
    {
        public IEnumerable<KafkaConnector> GetAllKafkaConnectors()
        {
            return GetKafkaConnectors();
        }

        public IEnumerable<KafkaConnector> GetKafkaConnectorsByTaskState(string taskState = "failed")
        {
            return GetKafkaConnectors().Where(c => c.TaskState.Equals(taskState, System.StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<KafkaConnector> RestartAllFailedKafkaConnectors()
        {
            var failedKafkaConnectors = GetKafkaConnectors().Where(c => c.TaskState.Equals("failed", System.StringComparison.OrdinalIgnoreCase));
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
                client.PatchNamespacedCustomObject(new V1Patch(patchStr, V1Patch.PatchType.MergePatch), Helper.StrimziGroup(), Helper.StrimziConnectorVersion(), failedKafkaConnector.Namespace, Helper.StrimziConnectorPlural(), failedKafkaConnector.Name);
            }

            return failedKafkaConnectors;
        }

        private List<KafkaConnector> GetKafkaConnectors()
        {
            var kafkaConnectors = new List<KafkaConnector>();

            var client = Helper.GetKubernetesClient();

            var clusterCustomObjects = ((JObject)client.ListClusterCustomObject(Helper.StrimziGroup(), Helper.StrimziConnectorVersion(), Helper.StrimziConnectorPlural())).SelectToken("items").Children();

            foreach (var clusterCustomObject in clusterCustomObjects)
            {
                var connectorName = clusterCustomObject.SelectToken("metadata.name").ToString();
                var connectorNamespace = clusterCustomObject.SelectToken("metadata.namespace").ToString();
                var connectorState = clusterCustomObject.SelectToken("status.connectorStatus.connector.state").ToString();

                var taskState = string.Empty;
                if (clusterCustomObject.SelectToken("status.connectorStatus").Children().Any(t => t.Path.Contains("status.connectorStatus.tasks")))
                {
                    taskState = clusterCustomObject.SelectToken("status.connectorStatus.tasks[0].state").ToString();
                }

                var taskTrace = string.Empty;
                if (clusterCustomObject.SelectToken("status.connectorStatus.tasks[0]").Children().Any(t => t.Path.Contains("status.connectorStatus.tasks[0].trace")))
                {
                    taskTrace = clusterCustomObject.SelectToken("status.connectorStatus.tasks[0].trace").ToString();
                }

                kafkaConnectors.Add(new KafkaConnector
                {
                    Name = connectorName,
                    Namespace = connectorNamespace,
                    ConnectorState = connectorState,
                    TaskState = taskState,
                    TaskTrace = taskTrace
                });
            }

            return kafkaConnectors;
        }
    }
}
