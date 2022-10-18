using System.Threading.Tasks;
using k8s;

namespace KubeStatus.Data
{
    public class LogService
    {
        private readonly IKubernetes kubernetesClient;

        public LogService(IKubernetes kubernetesClient)
        {
            this.kubernetesClient = kubernetesClient;
        }

        public async Task<System.IO.Stream> GetContainerLogsAsync(string pod, string container, string k8sNamespace = "default", int tail = 10)
        {
            try
            {
                var response = await kubernetesClient.CoreV1.ReadNamespacedPodLogWithHttpMessagesAsync(pod, k8sNamespace, container: container, tailLines: tail).ConfigureAwait(false);

                return response.Body;
            }
            catch
            {
                return null;
            }
        }
    }
}
