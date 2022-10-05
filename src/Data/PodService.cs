using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeStatus.Models;

namespace KubeStatus.Data
{
    public class PodService
    {
        public async Task<IEnumerable<Pod>> GetAllNamespacedPodsAsync(string k8sNamespace = "default")
        {
            var pods = new List<Pod>();

            var client = Helper.GetKubernetesClient();

            var list = await client.ListNamespacedPodAsync(k8sNamespace);
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
                    PodStatus = item.Status.Phase,
                    PodVolumes = item.Spec.Volumes.Select(v => v.Name).ToList(),
                    PodIPs = item.Status.PodIPs.Select(i => i.Ip).ToList(),
                    HostIP = item.Status.HostIP,
                    NodeName = item.Spec.NodeName,
                    InitContainers = SortContainersAndStatuses(item.Status.InitContainerStatuses, item.Spec.InitContainers),
                    Containers = SortContainersAndStatuses(item.Status.ContainerStatuses, item.Spec.Containers)
                });
            }

            return await Task.FromResult(pods.AsEnumerable());
        }

        private IList<V1ContainerAndStatus> SortContainersAndStatuses(IList<V1ContainerStatus> statuses, IList<V1Container> containers)
        {
            var list = new List<V1ContainerAndStatus>();

            if (containers != null && containers.Count > 0)
            {
                foreach (var container in containers.OrderBy(c => c.Name))
                {
                    var name = container.Name;
                    var status = statuses.Where(s => s.Name.Equals(name)).FirstOrDefault();
                    list.Add(new V1ContainerAndStatus { ContainerStatus = status, Container = container });
                }
            }
            else if (statuses != null && statuses.Count > 0)
            {
                foreach (var status in statuses.OrderBy(s => s.Name))
                {
                    list.Add(new V1ContainerAndStatus { ContainerStatus = status });
                }
            }

            return list;
        }
    }
}
