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
    public class TorDatabasesController(TorDatabaseService torDatabaseService) : ControllerBase
    {
        private readonly TorDatabaseService _torDatabaseService = torDatabaseService;

        [HttpGet]
        public async Task<IEnumerable<TorDatabase>> GetAllTorDatabasesAsync(string? k8sNamespace = null)
        {
            return await _torDatabaseService.GetAllTorDatabasesAsync(k8sNamespace);
        }

        [HttpGet("Database/{k8sNamespace}/{name}")]
        public async Task<TorDatabase?> GetTorDatabasesAsync(string k8sNamespace, string name)
        {
            return await _torDatabaseService.GetTorDatabaseAsync(name, k8sNamespace);
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpDelete("Database/{k8sNamespace}/{name}")]
        public async Task<IActionResult> DeleteTorDatabasesAsync(string k8sNamespace, string name)
        {
            TorDatabase? torDatabase = await _torDatabaseService.GetTorDatabaseAsync(name, k8sNamespace);

            if (torDatabase != null)
            {
                await _torDatabaseService.DeleteTorDatabaseAsync(name, k8sNamespace);
                return Ok();
            }

            return NotFound();
        }
    }
}
