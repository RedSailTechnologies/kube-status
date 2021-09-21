using k8s;
using k8s.Models;

namespace KubeStatus.Data
{
    public class NamespaceService
    {
        public V1NamespaceList GetAllNamespaces()
        {
            var client = Helper.GetKubernetesClient();

            var namespaces = client.ListNamespace();

            return namespaces;
        }
    }
}
