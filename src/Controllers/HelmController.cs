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
        public async Task<IEnumerable<HelmListItem>> GetHelmListAll(string k8sNamespace = "default")
        {
            return await _helmService.GetHelmListAll(k8sNamespace);
        }
    }
}
