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

        [HttpPut("rollback/{package}")]
        public async Task<string> HelmRollback(string package, string k8sNamespace = "default")
        {
            return await _helmService.HelmRollback(package, k8sNamespace);
        }

        [HttpDelete("uninstall/{package}")]
        public async Task<string> HelmUninstall(string package, string k8sNamespace = "default")
        {
            return await _helmService.HelmUninstall(package, k8sNamespace);
        }
    }
}
