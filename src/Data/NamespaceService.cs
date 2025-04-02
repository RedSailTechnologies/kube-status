using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;

namespace KubeStatus.Data
{
    public class NamespaceService(IKubernetes kubernetesClient)
    {
        private readonly IKubernetes _kubernetesClient = kubernetesClient;

        public async Task<V1NamespaceList> GetAllNamespacesAsync()
        {
            V1NamespaceList namespaces = await _kubernetesClient.CoreV1.ListNamespaceAsync();

            return await Task.FromResult(namespaces);
        }

        public async Task<IEnumerable<string>> GetAllNamespaceNamesAsync()
        {
            V1NamespaceList namespaces = await _kubernetesClient.CoreV1.ListNamespaceAsync();

            if (namespaces == null)
            {
                return await Task.FromResult(new List<string>());
            }

            return await Task.FromResult(namespaces.Items.Select(i => i.Metadata.Name));
        }
    }
}
