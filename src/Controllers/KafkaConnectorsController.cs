using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using KubeStatus.Data;
using KubeStatus.Models;

namespace KubeStatus.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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
        public async Task<IEnumerable<KafkaConnector>> GetAllKafkaConnectorsAsync()
        {
            return await _kafkaConnectorService.GetAllKafkaConnectorsAsync();
        }

        [HttpGet]
        [Route("status")]
        public async Task<string> GetConnectorsStatusAsync()
        {
            return await _kafkaConnectorService.GetConnectorsStatusAsync();
        }

        [HttpGet("{taskState}")]
        public async Task<IEnumerable<KafkaConnector>> GetKafkaConnectorsByTaskStateAsync(string taskState = "failed")
        {
            return await _kafkaConnectorService.GetKafkaConnectorsByTaskStateAsync(taskState);
        }

        [Authorize(Policy = "RequireEditorRole")]
        [HttpPatch("RestartFailed")]
        public async Task<IEnumerable<KafkaConnector>> RestartAllFailedKafkaConnectorsAsync()
        {
            return await _kafkaConnectorService.RestartAllFailedKafkaConnectorsAsync();
        }
    }
}
