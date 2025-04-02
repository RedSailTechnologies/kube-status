using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using k8s;
using KubeStatus.Models;
using Microsoft.Extensions.Caching.Memory;

namespace KubeStatus.Data
{
    public class SparkApplicationService(IKubernetes kubernetesClient, IMemoryCache memoryCache)
    {
        private readonly IKubernetes kubernetesClient = kubernetesClient;

        public IMemoryCache MemoryCache { get; } = memoryCache;

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public async Task<IEnumerable<SparkApplication>?> GetAllSparkApplicationsAsync()
        {
            return await GetSparkApplicationsAsync();
        }

        public Task<IEnumerable<SparkApplication>?> GetSparkApplicationsAsync(string? filterStatus = null)
        {
            var sparkApplications = new List<SparkApplication>();

            if (string.IsNullOrWhiteSpace(filterStatus))
            {
                filterStatus = "";
            }

            return MemoryCache.GetOrCreateAsync(filterStatus, async e =>
            {
                e.SetOptions(new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromSeconds(10)
                });

                var sparkApplications = new List<SparkApplication>();

                object response = await kubernetesClient.CustomObjects.ListClusterCustomObjectAsync(Helper.SparkGroup(), Helper.SparkApplicationVersion(), Helper.SparkApplicationPlural());

                string jsonString = JsonSerializer.Serialize(response);
                JsonNode jsonNode = JsonNode.Parse(jsonString)!;
                JsonNode itemsNode = jsonNode!["items"]!;

                foreach (JsonNode? item in itemsNode.AsArray())
                {
                    SparkApplicationStatus? status = JsonSerializer.Deserialize<SparkApplicationStatus>(item!["status"]!, _jsonSerializerOptions);
                    string applicationState = status?.ApplicationState?["state"] ?? "Unknown";

                    if (!string.IsNullOrWhiteSpace(filterStatus) && !filterStatus.Equals(applicationState, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string apiVersion = item!["apiVersion"]!.ToString();
                    string kind = item!["kind"]!.ToString();
                    Metadata? metadata = JsonSerializer.Deserialize<Metadata>(item!["metadata"]!, _jsonSerializerOptions);
                    string spec = item!["spec"]!.ToString();

                    sparkApplications.Add(new SparkApplication
                    {
                        ApiVersion = apiVersion,
                        Kind = kind,
                        Metadata = metadata,
                        Status = status,
                        Spec = spec
                    });
                }

                return sparkApplications.AsEnumerable();
            });
        }

        public async Task<int> DeleteFailedSparkApplicationsAsync(string keepHours = "168")
        {
            try
            {
                if (!double.TryParse(keepHours, out double hours))
                {
                    hours = 168;
                }
                hours *= -1;
                DateTime date = DateTime.UtcNow.AddHours(hours);

                IEnumerable<SparkApplication>? crs = await GetSparkApplicationsAsync("failed");
                int deleted = 0;
                if (crs != null)
                {
                    foreach (SparkApplication cr in crs)
                    {
                        if (cr != null && cr.Metadata != null)
                        {
                            if (cr.Metadata.CreationTimestamp.UtcDateTime < date)
                            {
                                await kubernetesClient.CustomObjects.DeleteNamespacedCustomObjectAsync(
                                    Helper.SparkGroup(),
                                    Helper.SparkApplicationVersion(),
                                    cr.Metadata.Namespace ?? "default",
                                    Helper.SparkApplicationPlural(),
                                    cr.Metadata.Name).ConfigureAwait(false);

                                deleted++;
                            }
                        }
                    }
                }
                return deleted;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return -1;
            }
        }
    }
}
