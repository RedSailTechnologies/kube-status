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
    public class HelmController : ControllerBase
    {
        private readonly ILogger<HelmController> _logger;
        private readonly HelmService _helmService;

        public HelmController(ILogger<HelmController> logger, HelmService helmService)
        {
            _logger = logger;
            _helmService = helmService;
        }

        [HttpGet("list/{k8sNamespace}")]
        public async Task<IEnumerable<HelmListItem>> HelmListAll(string k8sNamespace = "default")
        {
            return await _helmService.HelmListAll(k8sNamespace);
        }

        [Authorize(Policy = "RequireEditorRole")]
        [HttpPatch("rollback/{package}")]
        public async Task<string> HelmRollback(string package, string k8sNamespace = "default")
        {
            return await _helmService.HelmRollback(package, k8sNamespace);
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpDelete("uninstall/{package}")]
        public async Task<string> HelmUninstall(string package, string k8sNamespace = "default")
        {
            return await _helmService.HelmUninstall(package, k8sNamespace);
        }
    }
}
