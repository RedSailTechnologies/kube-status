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
    public class SparkApplicationsController : ControllerBase
    {
        private readonly ILogger<SparkApplicationsController> _logger;
        private readonly SparkApplicationService _SparkApplicationService;

        public SparkApplicationsController(ILogger<SparkApplicationsController> logger, SparkApplicationService SparkApplicationService)
        {
            _logger = logger;
            _SparkApplicationService = SparkApplicationService;
        }

        [HttpGet]
        public async Task<IEnumerable<SparkApplication>> GetAllSparkApplicationsAsync()
        {
            return await _SparkApplicationService.GetAllSparkApplicationsAsync();
        }

        [HttpGet("{filterStatus}")]
        public async Task<IEnumerable<SparkApplication>> GetSparkApplicationsAsync(string filterStatus = null)
        {
            return await _SparkApplicationService.GetSparkApplicationsAsync(filterStatus);
        }

        [HttpDelete("{keepDate}")]
        public async Task<IActionResult> DeleteFailedSparkApplicationsAsync(string keepDate = null)
        {
            var deleteResults = await _SparkApplicationService.DeleteFailedSparkApplicationsAsync(keepDate);
            if (deleteResults < 0)
            {
                return BadRequest();
            }
            else
            {
                return Ok(deleteResults);
            }
        }
    }
}
