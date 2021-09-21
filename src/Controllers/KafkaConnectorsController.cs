using System.Collections.Generic;
using KubeStatus.Models;
using KubeStatus.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
            var kafkaConnectorService = new KafkaConnectorService();
            return kafkaConnectorService.GetAllKafkaConnectors();
        }

        [HttpGet("{taskState}")]
        public IEnumerable<KafkaConnector> GetKafkaConnectorsByTaskState(string taskState = "failed")
        {
            var kafkaConnectorService = new KafkaConnectorService();
            return kafkaConnectorService.GetKafkaConnectorsByTaskState(taskState);
        }

        [HttpPatch("RestartFailed")]
        public IEnumerable<KafkaConnector> RestartAllFailedKafkaConnectors()
        {
            var kafkaConnectorService = new KafkaConnectorService();
            return kafkaConnectorService.RestartAllFailedKafkaConnectors();
        }
    }
}
