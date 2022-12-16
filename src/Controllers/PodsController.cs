using System.Threading.Tasks;
using KubeStatus.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace KubeStatus.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PodsController : ControllerBase
    {
        private readonly ILogger<PodsController> _logger;
        private readonly PodService _podService;

        public PodsController(ILogger<PodsController> logger, PodService podService)
        {
            _logger = logger;
            _podService = podService;
        }

        [HttpGet]
        public IActionResult ListNamespacedPodAsync()
        {
            return Redirect("/api/pods/default");
        }

        [HttpGet("{k8sNamespace}")]
        public async Task<IActionResult> GetAllNamespacedPodsAsync(string k8sNamespace = "default")
        {
            var pods = await _podService.GetAllNamespacedPodsAsync(k8sNamespace);

            if (pods == null)
            {
                return NotFound();
            }
            else
            {
                return Ok(pods);
            }
        }

        [HttpGet("{k8sNamespace}/{pod}/{container}/env")]
        public async Task<IActionResult> GetContainerEnvironmentVariablesAsync(string k8sNamespace, string pod, string container)
        {
            var envVars = await _podService.GetContainerEnvironmentVariablesAsync(k8sNamespace, pod, container);

            if (envVars == null)
            {
                return NotFound();
            }
            else
            {
                return Ok(envVars);
            }
        }

        [HttpGet("{k8sNamespace}/{pod}/{container}/logs")]
        public IActionResult GetContainerLogs(string k8sNamespace, string pod, string container, int tail = 100)
        {
            return Redirect($"/api/Logs/{k8sNamespace}/{pod}/{container}?tail={tail}");
        }

        [HttpGet("{k8sNamespace}/csv")]
        public async Task<IActionResult> GetAllNamespacedPodsFileAsync(string k8sNamespace = "default")
        {
            var fileName = $"{k8sNamespace}.csv";
            var bytes = await _podService.GetAllNamespacedPodsFileAsync(k8sNamespace);

            if (bytes == null)
            {
                return NotFound();
            }
            else
            {
                return File(bytes, "text/csv", fileName);
            }
        }

        [HttpGet("metrics")]
        public async Task<IActionResult> GetContainerLogsAsync(string podIP, int port)
        {
            var fileName = $"{podIP.Replace(".", "-")}-metrics.txt";
            var stream = await _podService.GetContainerMetricsAsync(podIP, port);

            if (stream == null)
            {
                return NotFound();
            }
            else
            {
                return File(stream, "application/octet-stream", fileName);
            }
        }
    }
}
