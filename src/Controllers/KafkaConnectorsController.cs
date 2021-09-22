using System.Collections.Generic;
using System.Threading.Tasks;
using KubeStatus.Data;
using KubeStatus.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace KubeStatus.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KafkaConnectorsController : ControllerBase
    {
        private readonly ILogger<KafkaConnectorsController> _logger;
        private readonly KafkaConnectorService _kafkaConnectorService;

        public KafkaConnectorsController(ILogger<KafkaConnectorsController> logger, KafkaConnectorService kafkaConnectorService)
        {
            _logger = logger;
            _kafkaConnectorService = kafkaConnectorService;
        }

        [HttpGet]
        public async Task<IEnumerable<KafkaConnector>> GetAllKafkaConnectors()
        {
            return await _kafkaConnectorService.GetAllKafkaConnectorsAsync();
        }

        [HttpGet("{taskState}")]
        public async Task<IEnumerable<KafkaConnector>> GetKafkaConnectorsByTaskState(string taskState = "failed")
        {
            return await _kafkaConnectorService.GetKafkaConnectorsByTaskStateAsync(taskState);
        }

        [HttpPatch("RestartFailed")]
        public async Task<IEnumerable<KafkaConnector>> RestartAllFailedKafkaConnectors()
        {
            return await _kafkaConnectorService.RestartAllFailedKafkaConnectorsAsync();
        }
    }
}
