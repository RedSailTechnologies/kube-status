using System.Collections.Generic;
using System.Linq;
using k8s;
using k8s.Models;
using KubeStatus.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace KubeStatus.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KafkaConnectorsController : ControllerBase
    {
        private readonly ILogger<KafkaConnectorsController> _logger;

        public KafkaConnectorsController(ILogger<KafkaConnectorsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<KafkaConnector> GetAllKafkaConnectors()
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

        [HttpGet("{taskState}")]
        public IEnumerable<KafkaConnector> GetKafkaConnectorsByTaskState(string taskState = "failed")
        {
            var kafkaConnectors = new List<KafkaConnector>();

            var client = Helper.GetKubernetesClient();

            var clusterCustomObjects = ((JObject)client.ListClusterCustomObject(Helper.StrimziGroup(), Helper.StrimziConnectorVersion(), Helper.StrimziConnectorPlural())).SelectToken("items").Children();

            foreach (var clusterCustomObject in clusterCustomObjects)
            {
                var connectorName = clusterCustomObject.SelectToken("metadata.name").ToString();
                var connectorNamespace = clusterCustomObject.SelectToken("metadata.namespace").ToString();
                var connectorState = clusterCustomObject.SelectToken("status.connectorStatus.connector.state").ToString();

                var currentTaskState = string.Empty;
                if (clusterCustomObject.SelectToken("status.connectorStatus").Children().Any(t => t.Path.Contains("status.connectorStatus.tasks")))
                {
                    currentTaskState = clusterCustomObject.SelectToken("status.connectorStatus.tasks[0].state").ToString();
                }

                var taskTrace = string.Empty;
                if (clusterCustomObject.SelectToken("status.connectorStatus.tasks[0]").Children().Any(t => t.Path.Contains("status.connectorStatus.tasks[0].trace")))
                {
                    taskTrace = clusterCustomObject.SelectToken("status.connectorStatus.tasks[0].trace").ToString();
                }

                if (currentTaskState.Equals(taskState, System.StringComparison.OrdinalIgnoreCase))
                {
                    kafkaConnectors.Add(new KafkaConnector
                    {
                        Name = connectorName,
                        Namespace = connectorNamespace,
                        ConnectorState = connectorState,
                        TaskState = currentTaskState,
                        TaskTrace = taskTrace
                    });
                }
            }

            return kafkaConnectors;
        }

        [HttpPatch("RestartFailed")]
        public IEnumerable<KafkaConnector> RestartAllFailedKafkaConnectors()
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

                if (taskState.Equals("failed", System.StringComparison.OrdinalIgnoreCase))
                {
                    kafkaConnectors.Add(new KafkaConnector
                    {
                        Name = connectorName,
                        Namespace = connectorNamespace,
                        ConnectorState = connectorState,
                        TaskState = taskState,
                        TaskTrace = taskTrace
                    });

                    var patchStr = @"
{
    ""metadata"": {
        ""annotations"": {
            ""strimzi.io/restart-task"": ""0""
        }
    }
}";

                    client.PatchNamespacedCustomObject(new V1Patch(patchStr, V1Patch.PatchType.MergePatch), Helper.StrimziGroup(), Helper.StrimziConnectorVersion(), connectorNamespace, Helper.StrimziConnectorPlural(), connectorName);
                }
            }

            return kafkaConnectors;
        }
    }
}
