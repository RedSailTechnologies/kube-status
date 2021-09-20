using k8s;
using k8s.Models;

namespace KubeStatus.Repository
{
    public class NamespacesRepository
    {
        public V1NamespaceList GetAllNamespaces()
        {
            var client = Helper.GetKubernetesClient();

            var namespaces = client.ListNamespace();

            return namespaces;
        }
    }
}
