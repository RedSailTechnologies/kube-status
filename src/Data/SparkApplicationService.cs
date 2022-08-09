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
        private static JsonSerializerOptions _options = new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public async Task<IEnumerable<SparkApplication>> GetAllSparkApplicationsAsync()
        {
            return await GetSparkApplications();
        }

        private async Task<IEnumerable<SparkApplication>> GetSparkApplications()
        {
            var sparkApplications = new List<SparkApplication>();

            var client = Helper.GetKubernetesClient();

            var response = await client.ListClusterCustomObjectAsync(Helper.SparkGroup(), Helper.SparkApplicationVersion(), Helper.SparkApplicationPlural());

            var jsonString = JsonSerializer.Serialize<object>(response);
            JsonNode jsonNode = JsonNode.Parse(jsonString)!;
            JsonNode itemsNode = jsonNode!["items"]!;

            foreach (var item in itemsNode.AsArray())
            {
                var apiVersion = item!["apiVersion"]!.ToString();
                var kind = item!["kind"]!.ToString();
                var metadata = JsonSerializer.Deserialize<Metadata>(item!["metadata"]!, _options);
                var status = JsonSerializer.Deserialize<SparkApplicationStatus>(item!["status"]!, _options);
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
