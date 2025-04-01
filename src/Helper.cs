using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;

using k8s;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace KubeStatus
{
    public static class Helper
    {
        static Dictionary<string, string> _podStatusDictionary;

        static Helper()
        {
            _podStatusDictionary = [];
        }

        public static void BuildPodStatusDictionary()
        {
            _podStatusDictionary.Add("All", "");
            _podStatusDictionary.Add("Pending", "Pending");
            _podStatusDictionary.Add("Running", "Running");
            _podStatusDictionary.Add("Succeeded", "Succeeded");
            _podStatusDictionary.Add("Failed", "Failed");
            _podStatusDictionary.Add("Unknown", "Unknown");
        }

        public static Dictionary<string, string> ReturnPodStatusDictionary()
        {
            return _podStatusDictionary;
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

        public static bool ShowMetricsDownload()
        {
            return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("POD_METRIC_PORT_PAGE"));
        }

        public static string MetricsPortName()
        {
            return (Environment.GetEnvironmentVariable("POD_METRIC_PORT_PAGE") ?? string.Empty).Split("|")[0];
        }

        public static string MetricsRoute()
        {
            return (Environment.GetEnvironmentVariable("POD_METRIC_PORT_PAGE") ?? string.Empty).Split("|")[1];
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

        public static string GetUserIdentityName(this IHttpContextAccessor httpContextAccessor)
        {
            return httpContextAccessor.HttpContext?.User.Identity?.Name?.ToLower() ?? "";
        }
    }
}
