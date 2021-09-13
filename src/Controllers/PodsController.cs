using k8s;
using k8s.Models;
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
        public V1PodList ListNamespacedPod(string k8sNamespace = "default")
        {
            var client = Helper.GetKubernetesClient();
            return client.ListNamespacedPod(k8sNamespace);
        }
    }
}
