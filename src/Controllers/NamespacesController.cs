using System.Threading.Tasks;
using k8s.Models;
using KubeStatus.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace KubeStatus.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NamespacesController : ControllerBase
    {
        private readonly ILogger<NamespacesController> _logger;
        private readonly NamespaceService _namespaceService;

        public NamespacesController(ILogger<NamespacesController> logger, NamespaceService namespaceService)
        {
            _logger = logger;
            _namespaceService = namespaceService;
        }

        [HttpGet]
        public async Task<V1NamespaceList> GetAllNamespaces()
        {
            return await _namespaceService.GetAllNamespacesAsync();
        }
    }
}
