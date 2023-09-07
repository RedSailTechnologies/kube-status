using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using KubeStatus.Data;

namespace KubeStatus.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    public class DeploymentsController : ControllerBase
    {
        private readonly ILogger<DeploymentsController> _logger;
        private readonly DeploymentService _deploymentService;

        public DeploymentsController(ILogger<DeploymentsController> logger, DeploymentService deploymentService)
        {
            _logger = logger;
            _deploymentService = deploymentService;
        }

        [HttpGet]
        public IActionResult ListNamespacedDeploymentAsync()
        {
            return Redirect("/api/Deployments/default");
        }

        [HttpGet("{k8sNamespace}")]
        public async Task<IActionResult> GetAllNamespacedDeploymentsAsync(string k8sNamespace = "default")
        {
            var deployments = await _deploymentService.GetAllNamespacedDeploymentsAsync(k8sNamespace);

            if (deployments == null)
            {
                return NotFound();
            }
            else
            {
                return Ok(deployments);
            }
        }

        [Authorize(Policy = "RequireEditorRole")]
        [HttpPatch("restart/{name}")]
        public async Task<IActionResult> RestartDeploymentAsync(string name, string k8sNamespace = "default")
        {
            var restarted = await _deploymentService.RestartDeploymentAsync(name, k8sNamespace);

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
        public async Task<IActionResult> RestartNamespacedDeploymentAsync(string k8sNamespace = "default")
        {
            var restarted = await _deploymentService.RestartNamespacedDeploymentAsync(k8sNamespace);

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
        public async Task<IActionResult> ScaleDeploymentAsync(string name, int replicas, string k8sNamespace = "default")
        {
            var scaled = await _deploymentService.ScaleDeploymentAsync(name, replicas, k8sNamespace);

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
