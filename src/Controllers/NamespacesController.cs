using k8s;
using k8s.Models;
using KubeStatus.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace KubeStatus.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NamespacesController : ControllerBase
    {
        private readonly ILogger<NamespacesController> _logger;

        public NamespacesController(ILogger<NamespacesController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public V1NamespaceList GetAllNamespaces()
        {
            var namespacesRepository = new NamespacesRepository();
            return namespacesRepository.GetAllNamespaces();
        }
    }
}
