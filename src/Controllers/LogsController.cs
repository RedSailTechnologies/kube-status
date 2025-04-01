using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using KubeStatus.Data;

namespace KubeStatus.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController(LogService logService) : ControllerBase
    {
        private readonly LogService _logService = logService;

        [HttpGet("{k8sNamespace}/{pod}/{container}")]
        public async Task<IActionResult> GetContainerLogsAsync(string k8sNamespace, string pod, string container, int tail = 100)
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
