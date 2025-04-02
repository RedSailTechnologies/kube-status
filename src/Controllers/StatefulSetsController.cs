using System.Threading.Tasks;
using KubeStatus.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KubeStatus.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    public class StatefulSetsController(StatefulSetService StatefulSetService) : ControllerBase
    {
        private readonly StatefulSetService _StatefulSetService = StatefulSetService;

        [HttpGet]
        public IActionResult ListNamespacedStatefulSetAsync()
        {
            return Redirect("/api/StatefulSets/default");
        }

        [HttpGet("{k8sNamespace}")]
        public async Task<IActionResult> GetAllNamespacedStatefulSetsAsync(string k8sNamespace = "default")
        {
            k8s.Models.V1StatefulSetList? StatefulSets = await _StatefulSetService.GetAllNamespacedStatefulSetsAsync(k8sNamespace);

            if (StatefulSets == null)
            {
                return NotFound();
            }
            else
            {
                return Ok(StatefulSets);
            }
        }

        [Authorize(Policy = "RequireEditorRole")]
        [HttpPatch("restart/{name}")]
        public async Task<IActionResult> RestartStatefulSetAsync(string name, string k8sNamespace = "default")
        {
            bool restarted = await _StatefulSetService.RestartStatefulSetAsync(name, k8sNamespace);

            if (restarted)
            {
                return Ok();
            }
            else
            {
                return Problem();
            }
        }

        [Authorize(Policy = "RequireEditorRole")]
        [HttpPatch("rollout/restart")]
        public async Task<IActionResult> RestartNamespacedStatefulSetAsync(string k8sNamespace = "default")
        {
            bool restarted = await _StatefulSetService.RestartNamespacedStatefulSetAsync(k8sNamespace);

            if (restarted)
            {
                return Ok();
            }
            else
            {
                return Problem();
            }
        }

        [Authorize(Policy = "RequireEditorRole")]
        [HttpPatch("scale/{name}/{replicas}")]
        public async Task<IActionResult> ScaleStatefulSetAsync(string name, int replicas, string k8sNamespace = "default")
        {
            bool scaled = await _StatefulSetService.ScaleStatefulSetAsync(name, replicas, k8sNamespace);

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
