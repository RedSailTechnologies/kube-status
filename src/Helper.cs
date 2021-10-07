using System;

using k8s;

namespace KubeStatus
{
    public static class Helper
    {
        public static KubernetesClientConfiguration GetKubernetesClientConfiguration()
        {
            if (bool.TryParse(Environment.GetEnvironmentVariable("BUILD_CONFIG_FROM_CONFIG_FILE"), out bool localContext) && localContext)
            {
                Console.WriteLine("Using kube.config");
                return KubernetesClientConfiguration.BuildConfigFromConfigFile();
            }

            Console.WriteLine("Using in cluster config");
            return KubernetesClientConfiguration.InClusterConfig();
        }

        public static Kubernetes GetKubernetesClient()
        {
            return new Kubernetes(GetKubernetesClientConfiguration());
        }

        public static bool EnableSwagger()
        {
            bool.TryParse(Environment.GetEnvironmentVariable("ENABLE_SWAGGER"), out bool enableSwagger);
            return enableSwagger;
        }

        public static string StrimziGroup()
        {
            return Environment.GetEnvironmentVariable("STRIMZI__GROUP") ?? "kafka.strimzi.io";
        }

        public static string StrimziConnectorVersion()
        {
            return Environment.GetEnvironmentVariable("STRIMZI__CONNECTOR_VERSION") ?? "v1beta2";
        }

        public static string StrimziConnectorPlural()
        {
            return Environment.GetEnvironmentVariable("STRIMZI__CONNECTOR_PLURAL") ?? "kafkaconnectors";
        }
    }
}
