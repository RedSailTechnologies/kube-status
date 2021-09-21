using System.Collections.Generic;
using KubeStatus.Models;
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

        public PodsController(ILogger<PodsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult ListNamespacedPod()
        {
            return Redirect("/api/pods/default");
        }

        [HttpGet("{k8sNamespace}")]
        public IEnumerable<Pod> GetAllNamespacedPods(string k8sNamespace = "default")
        {
            var podService = new PodService();
            return podService.GetAllNamespacedPods(k8sNamespace);
        }
    }
}
