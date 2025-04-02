using System;
using System.Collections.Generic;
using System.Linq;
using k8s;
using Microsoft.AspNetCore.Http;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace KubeStatus
{
    public static class Helper
    {
        private static readonly Dictionary<string, string> s_podStatusDictionary;

        static Helper()
        {
            s_podStatusDictionary = [];
        }

        public static void BuildPodStatusDictionary()
        {
            s_podStatusDictionary.Add("All", "");
            s_podStatusDictionary.Add("Pending", "Pending");
            s_podStatusDictionary.Add("Running", "Running");
            s_podStatusDictionary.Add("Succeeded", "Succeeded");
            s_podStatusDictionary.Add("Failed", "Failed");
            s_podStatusDictionary.Add("Unknown", "Unknown");
        }

        public static Dictionary<string, string> ReturnPodStatusDictionary()
        {
            return s_podStatusDictionary;
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
            _ = bool.TryParse(Environment.GetEnvironmentVariable("ENABLE_SWAGGER"), out bool enableSwagger);
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

        public static string TorGroup()
        {
            return Environment.GetEnvironmentVariable("TOR__GROUP") ?? "redsail.tor";
        }

        public static string TorEnterpriseVersion()
        {
            return Environment.GetEnvironmentVariable("TOR__ENTERPRISE_VERSION") ?? "v1";
        }

        public static string TorEnterprisePlural()
        {
            return Environment.GetEnvironmentVariable("TOR__ENTERPRISE_PLURAL") ?? "enterprises";
        }

        public static string TorDatabaseVersion()
        {
            return Environment.GetEnvironmentVariable("TOR__DATABASE_VERSION") ?? "v1";
        }

        public static string TorDatabasePlural()
        {
            return Environment.GetEnvironmentVariable("TOR__DATABASE_PLURAL") ?? "databases";
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

            ISerializer serializer = new SerializerBuilder()
                .ConfigureDefaultValuesHandling(defaultValueHandlingEnum)
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            string yaml = serializer.Serialize(obj);

            return yaml;
        }

        public static string GetUserIdentityName(this IHttpContextAccessor httpContextAccessor)
        {
            return httpContextAccessor.HttpContext?.User.Identity?.Name?.ToLower() ?? "";
        }

        public static bool IsPrivateIP(string ipAddress)
        {
            int[] ipParts = [.. ipAddress.Split(["."], StringSplitOptions.RemoveEmptyEntries).Select(s => int.Parse(s))];

            if (ipParts[0] == 10 || (ipParts[0] == 192 && ipParts[1] == 168) || (ipParts[0] == 172 && ipParts[1] >= 16 && ipParts[1] <= 31))
            {
                return true;
            }

            return false;
        }

        public static bool IsValidPort(int port)
        {
            if (port >= 0 && port <= 65535)
            {
                return true;
            }

            return false;
        }
    }
}
