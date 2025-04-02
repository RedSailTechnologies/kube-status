using System.Collections.Generic;
using System.Threading.Tasks;
using KubeStatus.Data;
using KubeStatus.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KubeStatus.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    public class KafkaConnectorsController(KafkaConnectorService kafkaConnectorService) : ControllerBase
    {
        private readonly KafkaConnectorService _kafkaConnectorService = kafkaConnectorService;

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

        [Authorize(Policy = "RequireAdminRole")]
        [HttpPatch("Restart/{connectorName}")]
        public async Task<KafkaConnector> RestartKafkaConnectorAsync(string connectorName, string k8sNamespace = "default")
        {
            return await _kafkaConnectorService.RestartKafkaConnectorAsync(connectorName, k8sNamespace);
        }
    }
}
