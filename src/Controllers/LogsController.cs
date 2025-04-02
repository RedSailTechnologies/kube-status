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
    public class LogsController(LogService logService) : ControllerBase
    {
        private readonly LogService _logService = logService;

        [HttpGet("{k8sNamespace}/{pod}/{container}")]
        public async Task<IActionResult> GetContainerLogsAsync(string k8sNamespace, string pod, string container, int tail = 100)
        {
            string fileName = $"{k8sNamespace}-{pod}-{container}.log";
            System.IO.Stream? stream = await _logService.GetContainerLogsAsync(pod, container, k8sNamespace, tail);

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
