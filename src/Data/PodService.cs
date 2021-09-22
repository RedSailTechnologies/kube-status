using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using KubeStatus.Models;

namespace KubeStatus.Data
{
    public class PodService
    {
        public Task<IEnumerable<Pod>> GetAllNamespacedPodsAsync(string k8sNamespace = "default")
        {
            var pods = new List<Pod>();

            var client = Helper.GetKubernetesClient();

            var list = client.ListNamespacedPod(k8sNamespace);
            foreach (var item in list.Items)
            {
                pods.Add(new Pod
                {
                    Name = item.Metadata.Labels.ContainsKey("app.kubernetes.io/name") ? item.Metadata.Labels["app.kubernetes.io/name"] : "",
                    Instance = item.Metadata.Labels.ContainsKey("app.kubernetes.io/instance") ? item.Metadata.Labels["app.kubernetes.io/instance"] : "",
                    Version = item.Metadata.Labels.ContainsKey("app.kubernetes.io/version") ? item.Metadata.Labels["app.kubernetes.io/version"] : "",
                    Component = item.Metadata.Labels.ContainsKey("app.kubernetes.io/component") ? item.Metadata.Labels["app.kubernetes.io/component"] : "",
                    PartOf = item.Metadata.Labels.ContainsKey("app.kubernetes.io/part-of") ? item.Metadata.Labels["app.kubernetes.io/part-of"] : "",
                    ManagedBy = item.Metadata.Labels.ContainsKey("app.kubernetes.io/managed-by") ? item.Metadata.Labels["app.kubernetes.io/managed-by"] : "",
                    CreatedBy = item.Metadata.Labels.ContainsKey("app.kubernetes.io/created-by") ? item.Metadata.Labels["app.kubernetes.io/created-by"] : "",
                    PodName = item.Metadata.Name,
                    Namespace = k8sNamespace,
                    Labels = item.Metadata.Labels,
                    Annotations = item.Metadata.Annotations,
                    Status = item.Status.ContainerStatuses,
                    PodStatus = item.Status.Phase,
                    PodVolumes = item.Spec.Volumes.Select(v => v.Name).ToList()
                });
            }

            return Task.FromResult(pods.AsEnumerable());
        }
    }
}
