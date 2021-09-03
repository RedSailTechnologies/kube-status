using System.IO.Pipes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using k8s;

namespace KubeStatus.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PodsController : ControllerBase
    {
        private readonly ILogger<PodsController> _logger;

        public PodsController(ILogger<PodsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<Pod> GetAllPods()
        {
            var pods = new List<Pod>();

            var client = Helper.GetKubernetesClient();

            var namespaces = client.ListNamespace();

            foreach (var ns in namespaces.Items)
            {
                var list = client.ListNamespacedPod(ns.Metadata.Name);
                foreach (var item in list.Items)
                {
                    pods.Add(new Pod
                    {
                        Name = item.Metadata.Name,
                        Namespace = ns.Metadata.Name,
                        Version = item.Metadata.Labels.ContainsKey("app.kubernetes.io/version") ? item.Metadata.Labels["app.kubernetes.io/version"] : "",
                        Labels = item.Metadata.Labels,
                        Annotations = item.Metadata.Annotations,
                        Status = item.Status.ContainerStatuses
                    });
                }
            }

            return pods;
        }

        [HttpGet("{k8sNamespace}")]
        public IEnumerable<Pod> GetAllNamespacedPods(string k8sNamespace = "default")
        {
            var pods = new List<Pod>();

            var client = Helper.GetKubernetesClient();

            var list = client.ListNamespacedPod(k8sNamespace);
            foreach (var item in list.Items)
            {
                pods.Add(new Pod
                {
                    Name = item.Metadata.Name,
                    Namespace = k8sNamespace,
                    Version = item.Metadata.Labels.ContainsKey("app.kubernetes.io/version") ? item.Metadata.Labels["app.kubernetes.io/version"] : "",
                    Labels = item.Metadata.Labels,
                    Annotations = item.Metadata.Annotations,
                    Status = item.Status.ContainerStatuses
                });
            }

            return pods;
        }
    }
}
