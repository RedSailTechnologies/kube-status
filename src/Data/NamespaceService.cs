using System.Threading.Tasks;
using k8s;
using k8s.Models;

namespace KubeStatus.Data
{
    public class NamespaceService
    {
        public Task<V1NamespaceList> GetAllNamespacesAsync()
        {
            var client = Helper.GetKubernetesClient();

            var namespaces = client.ListNamespace();

            return Task.FromResult(namespaces);
        }
    }
}
