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
        public IActionResult ListNamespacedPod()
        {
            return Redirect("/api/pods/default");
        }

        [HttpGet("{k8sNamespace}")]
        public async Task<IEnumerable<Pod>> GetAllNamespacedPods(string k8sNamespace = "default")
        {
            return await _podService.GetAllNamespacedPodsAsync(k8sNamespace);
        }
    }
}
