using System.Threading.Tasks;

namespace KubeStatus.Data
{
    public class LogService
    {
        public async Task<System.IO.Stream> GetContainerLogsAsync(string pod, string container, string k8sNamespace = "default", int tail = 10)
        {
            var client = Helper.GetKubernetesClient();

            try
            {
                var response = await client.CoreV1.ReadNamespacedPodLogWithHttpMessagesAsync(pod, k8sNamespace, container: container, tailLines: tail).ConfigureAwait(false);

                return response.Body;
            }
            catch
            {
                return null;
            }
        }
    }
}
