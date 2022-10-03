using System;

using k8s;

namespace KubeStatus
{
    public static class Helper
    {
        public static string GetBaseUrl()
        {
            var baseUrl = "http://localhost:8080/";
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
            {
                var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS").Split(";");
                var protocol = urls[0].Split(":")[0];
                var port = urls[0].Split(":")[2];
                if (port.EndsWith("/"))
                {
                    port = port.Remove(port.Length - 1);
                }
                baseUrl = $"{protocol}://localhost:{port}/";
            }
            return baseUrl;
        }

        public static KubernetesClientConfiguration GetKubernetesClientConfiguration()
        {
            if (bool.TryParse(Environment.GetEnvironmentVariable("BUILD_CONFIG_FROM_CONFIG_FILE"), out bool localContext) && localContext)
            {
                return KubernetesClientConfiguration.BuildConfigFromConfigFile();
            }

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

        public static string StrimziConnectClusterServiceHost()
        {
            return Environment.GetEnvironmentVariable("STRIMZI__CONNECT_CLUSTER_SERVICE_HOST") ?? "";
        }

        public static string SparkGroup()
        {
            return Environment.GetEnvironmentVariable("SPARK__GROUP") ?? "sparkoperator.k8s.io";
        }

        public static string SparkApplicationVersion()
        {
            return Environment.GetEnvironmentVariable("SPARK__APPLICATION_VERSION") ?? "v1beta2";
        }

        public static string SparkApplicationPlural()
        {
            return Environment.GetEnvironmentVariable("SPARK__APPLICATION_PLURAL") ?? "sparkapplications";
        }
    }
}
