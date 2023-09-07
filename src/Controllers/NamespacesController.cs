using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using k8s.Models;

using KubeStatus.Data;

namespace KubeStatus.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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
        public async Task<V1NamespaceList> GetAllNamespacesAsync()
        {
            return await _namespaceService.GetAllNamespacesAsync();
        }
    }
}
