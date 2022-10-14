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
    }
}
