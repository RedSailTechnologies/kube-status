using System;
using System.IO;
using System.Threading.Tasks;

using k8s;

namespace KubeStatus.Data
{
    public class LogService(IKubernetes kubernetesClient)
    {
        private readonly IKubernetes kubernetesClient = kubernetesClient;

        public async Task<Stream?> GetContainerLogsAsync(string pod, string container, string k8sNamespace = "default", int tail = 10)
        {
            try
            {
                k8s.Autorest.HttpOperationResponse<Stream> response = await kubernetesClient.CoreV1.ReadNamespacedPodLogWithHttpMessagesAsync(pod, k8sNamespace, container: container, tailLines: tail).ConfigureAwait(false);

                return response.Body;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}
