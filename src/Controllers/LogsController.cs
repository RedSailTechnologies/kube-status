using System.Threading.Tasks;
using KubeStatus.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace KubeStatus.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly ILogger<LogsController> _logger;
        private readonly LogService _logService;

        public LogsController(ILogger<LogsController> logger, LogService logService)
        {
            _logger = logger;
            _logService = logService;
        }

        [HttpGet("{k8sNamespace}/{pod}/{container}")]
        public async Task<IActionResult> GetAllNamespacedPodsAsync(string pod, string container, string k8sNamespace = "default", int tail = 100)
        {
            var fileName = $"{k8sNamespace}-{pod}-{container}.log";
            var stream = await _logService.GetContainerLogsAsync(pod, container, k8sNamespace, tail);

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
