using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using k8s;
using KubeStatus.Models;

namespace KubeStatus.Data
{
    public class SparkApplicationService
    {
        private readonly IKubernetes kubernetesClient;

        public SparkApplicationService(IKubernetes kubernetesClient)
        {
            this.kubernetesClient = kubernetesClient;
        }

        private static JsonSerializerOptions _options = new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public async Task<IEnumerable<SparkApplication>> GetAllSparkApplicationsAsync()
        {
            return await GetSparkApplicationsAsync();
        }

        public async Task<IEnumerable<SparkApplication>> GetSparkApplicationsAsync(string filterStatus = null)
        {
            var sparkApplications = new List<SparkApplication>();

            var response = await kubernetesClient.CustomObjects.ListClusterCustomObjectAsync(Helper.SparkGroup(), Helper.SparkApplicationVersion(), Helper.SparkApplicationPlural());

            var jsonString = JsonSerializer.Serialize<object>(response);
            JsonNode jsonNode = JsonNode.Parse(jsonString)!;
            JsonNode itemsNode = jsonNode!["items"]!;

            foreach (var item in itemsNode.AsArray())
            {
                var status = JsonSerializer.Deserialize<SparkApplicationStatus>(item!["status"]!, _options);
                var applicationState = status.ApplicationState["state"];

                if (!string.IsNullOrWhiteSpace(filterStatus) && !filterStatus.Equals(applicationState, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var apiVersion = item!["apiVersion"]!.ToString();
                var kind = item!["kind"]!.ToString();
                var metadata = JsonSerializer.Deserialize<Metadata>(item!["metadata"]!, _options);
                var spec = item!["spec"]!.ToString();

                sparkApplications.Add(new SparkApplication
                {
                    ApiVersion = apiVersion,
                    Kind = kind,
                    Metadata = metadata,
                    Status = status,
                    Spec = spec
                });
            }

            return sparkApplications;
        }
    }
}
