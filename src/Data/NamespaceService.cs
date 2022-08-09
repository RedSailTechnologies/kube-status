using System;
using System.Threading.Tasks;
using k8s;
using k8s.Models;

namespace KubeStatus.Data
{
    public class NamespaceService
    {
        public async Task<V1NamespaceList> GetAllNamespacesAsync()
        {
            var client = Helper.GetKubernetesClient();

            var namespaces = await client.ListNamespaceAsync();

            return await Task.FromResult(namespaces);
        }
    }
}
