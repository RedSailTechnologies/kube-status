using System;
using System.IO;
using System.Threading.Tasks;
using k8s;
using Microsoft.Extensions.Logging;

namespace KubeStatus.Data
{
    public class LogService(ILogger<LogService> logger, IKubernetes kubernetesClient)
    {
        private readonly ILogger<LogService> _logger = logger;
        private readonly IKubernetes _kubernetesClient = kubernetesClient;

        public async Task<Stream?> GetContainerLogsAsync(string pod, string container, string k8sNamespace = "default", int tail = 10)
        {
            try
            {
                k8s.Autorest.HttpOperationResponse<Stream> response = await _kubernetesClient.CoreV1.ReadNamespacedPodLogWithHttpMessagesAsync(pod, k8sNamespace, container: container, tailLines: tail).ConfigureAwait(false);

                return response.Body;
            }
            catch (Exception ex)
            {
                _logger.LogError("{ex}", ex.Message);
                return null;
            }
        }
    }
}
