using System.Threading.Tasks;
using KubeStatus.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace KubeStatus.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatefulSetsController : ControllerBase
    {
        private readonly ILogger<StatefulSetsController> _logger;
        private readonly StatefulSetService _StatefulSetService;

        public StatefulSetsController(ILogger<StatefulSetsController> logger, StatefulSetService StatefulSetService)
        {
            _logger = logger;
            _StatefulSetService = StatefulSetService;
        }

        [HttpGet]
        public IActionResult ListNamespacedStatefulSetAsync()
        {
            return Redirect("/api/StatefulSets/default");
        }

        [HttpGet("{k8sNamespace}")]
        public async Task<IActionResult> GetAllNamespacedStatefulSetsAsync(string k8sNamespace = "default")
        {
            var StatefulSets = await _StatefulSetService.GetAllNamespacedStatefulSetsAsync(k8sNamespace);

            if (StatefulSets == null)
            {
                return NotFound();
            }
            else
            {
                return Ok(StatefulSets);
            }
        }

        [HttpPatch("restart/{name}")]
        public async Task<IActionResult> RestartStatefulSetAsync(string name, string k8sNamespace = "default")
        {
            var restarted = await _StatefulSetService.RestartStatefulSetAsync(name, k8sNamespace);

            if (restarted)
            {
                return Ok();
            }
            else
            {
                return Problem();
            }
        }

        [HttpPatch("rollout/restart")]
        public async Task<IActionResult> RestartNamespacedStatefulSetAsync(string k8sNamespace = "default")
        {
            var restarted = await _StatefulSetService.RestartNamespacedStatefulSetAsync(k8sNamespace);

            if (restarted)
            {
                return Ok();
            }
            else
            {
                return Problem();
            }
        }

        [HttpPatch("scale/{name}/{replicas}")]
        public async Task<IActionResult> ScaleStatefulSetAsync(string name, int replicas, string k8sNamespace = "default")
        {
            var scaled = await _StatefulSetService.ScaleStatefulSetAsync(name, replicas, k8sNamespace);

            if (scaled)
            {
                return Ok();
            }
            else
            {
                return Problem();
            }
        }
    }
}
