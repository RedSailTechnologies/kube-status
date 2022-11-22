using System;
using k8s;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace KubeStatus
{
    public static class Helper
    {
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

        public static string GetHelmCaOrBypass()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("KUBE_CA_FILE")))
            {
                return "--kube-insecure-skip-tls-verify";
            }
            else
            {
                return $"--kube-ca-file {Environment.GetEnvironmentVariable("KUBE_CA_FILE")}";
            }
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

        /// <summary>
        /// Converts an object to yaml.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="defaultValueHandling">Available Options: OmitDefaults, OmitEmptyCollections, OmitNull, Preserve</param>
        /// <returns></returns>
        public static string ToYaml(this object obj, string defaultValueHandling = "OmitNull")
        {
            if (!Enum.TryParse(defaultValueHandling, out DefaultValuesHandling defaultValueHandlingEnum))
            {
                defaultValueHandlingEnum = DefaultValuesHandling.OmitNull;
            }

            var serializer = new SerializerBuilder()
                .ConfigureDefaultValuesHandling(defaultValueHandlingEnum)
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var yaml = serializer.Serialize(obj);

            return yaml;
        }
    }
}
