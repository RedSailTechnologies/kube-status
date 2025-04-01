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
    public class TorEnterprisesController(TorEnterpriseService torEnterpriseService) : ControllerBase
    {
        private readonly TorEnterpriseService _torEnterpriseService = torEnterpriseService;

        [HttpGet]
        public async Task<IEnumerable<TorEnterprise>> GetAllTorEnterprisesAsync(string? k8sNamespace = null)
        {
            return await _torEnterpriseService.GetAllTorEnterprisesAsync(k8sNamespace);
        }

        [HttpGet("Enterprise/{k8sNamespace}/{name}")]
        public async Task<TorEnterprise?> GetTorEnterprisesAsync(string k8sNamespace, string name)
        {
            return await _torEnterpriseService.GetTorEnterpriseAsync(name, k8sNamespace);
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpDelete("Enterprise/{k8sNamespace}/{name}")]
        public async Task<IActionResult> DeleteTorEnterprisesAsync(string k8sNamespace, string name)
        {
            var torEnterprise = await _torEnterpriseService.GetTorEnterpriseAsync(name, k8sNamespace);

            if (torEnterprise != null)
            {
                await _torEnterpriseService.DeleteTorEnterpriseAsync(name, k8sNamespace);
                return Ok();
            }

            return NotFound();
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpPatch("Trigger/{torEnterpriseName}")]
        public async Task<IActionResult> TriggerTorEnterpriseProcessingAsync(string torEnterpriseName, string k8sNamespace = "default")
        {
            var torEnterprise = await _torEnterpriseService.TriggerTorEnterpriseProcessingAsync(torEnterpriseName, k8sNamespace);

            if (torEnterprise != null)
            {
                return Ok(torEnterprise);
            }

            return NotFound();
        }
    }
}
