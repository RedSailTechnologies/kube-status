using System.Collections.Generic;
using KubeStatus.Models;
using KubeStatus.Repository;
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
            var kafkaConnectorsRepository = new KafkaConnectorsRepository();
            return kafkaConnectorsRepository.GetAllKafkaConnectors();
        }

        [HttpGet("{taskState}")]
        public IEnumerable<KafkaConnector> GetKafkaConnectorsByTaskState(string taskState = "failed")
        {
            var kafkaConnectorsRepository = new KafkaConnectorsRepository();
            return kafkaConnectorsRepository.GetKafkaConnectorsByTaskState(taskState);
        }

        [HttpPatch("RestartFailed")]
        public IEnumerable<KafkaConnector> RestartAllFailedKafkaConnectors()
        {
            var kafkaConnectorsRepository = new KafkaConnectorsRepository();
            return kafkaConnectorsRepository.RestartAllFailedKafkaConnectors();
        }
    }
}
