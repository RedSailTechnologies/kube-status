using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using KubeStatus.Data;
using KubeStatus.Models;

namespace KubeStatus.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    public class SparkApplicationsController(SparkApplicationService SparkApplicationService) : ControllerBase
    {
        private readonly SparkApplicationService _SparkApplicationService = SparkApplicationService;

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

        [Authorize(Policy = "RequireAdminRole")]
        [HttpDelete("{keepHours}")]
        public async Task<IActionResult> DeleteFailedSparkApplicationsAsync(string keepHours = "168")
        {
            var deleteResults = await _SparkApplicationService.DeleteFailedSparkApplicationsAsync(keepHours);
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
