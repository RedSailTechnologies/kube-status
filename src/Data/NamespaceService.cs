using System;
using System.Threading.Tasks;
using k8s;
using k8s.Models;

namespace KubeStatus.Data
{
    public class NamespaceService
    {
        private readonly IKubernetes kubernetesClient;

        public NamespaceService(IKubernetes kubernetesClient)
        {
            this.kubernetesClient = kubernetesClient;
        }

        public async Task<V1NamespaceList> GetAllNamespacesAsync()
        {
            var namespaces = await kubernetesClient.CoreV1.ListNamespaceAsync();

            return await Task.FromResult(namespaces);
        }
    }
}
