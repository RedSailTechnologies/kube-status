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
            Console.WriteLine("Fetching Client");
            var client = Helper.GetKubernetesClient();
            Console.WriteLine("Client Fetched");

            Console.WriteLine("Fetching Namespaces");
            var namespaces = client.ListNamespace();
            Console.WriteLine("Namespaces Fetched");

            foreach (var item in namespaces.Items)
            {
                Console.WriteLine(item.Metadata.Name);
            }

            if (namespaces.Items.Count == 0)
            {
                Console.WriteLine("Empty!");
            }

            return await Task.FromResult(namespaces);
        }
    }
}
