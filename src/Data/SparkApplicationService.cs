using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web;
using k8s;
using KubeStatus.Models;
using Microsoft.Extensions.Caching.Memory;

namespace KubeStatus.Data
{
    public class SparkApplicationService
    {
        private readonly IKubernetes kubernetesClient;

        public IMemoryCache MemoryCache { get; }

        public SparkApplicationService(IKubernetes kubernetesClient, IMemoryCache memoryCache)
        {
            this.kubernetesClient = kubernetesClient;
            MemoryCache = memoryCache;
        }

        private static JsonSerializerOptions _options = new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public async Task<IEnumerable<SparkApplication>> GetAllSparkApplicationsAsync()
        {
            return await GetSparkApplicationsAsync();
        }

        public Task<IEnumerable<SparkApplication>> GetSparkApplicationsAsync(string filterStatus = null)
        {
            return MemoryCache.GetOrCreateAsync(filterStatus, async e =>
            {
                e.SetOptions(new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromSeconds(10)
                });

                var sparkApplications = new List<SparkApplication>();

                var response = await kubernetesClient.CustomObjects.ListClusterCustomObjectAsync(Helper.SparkGroup(), Helper.SparkApplicationVersion(), Helper.SparkApplicationPlural());

                var jsonString = JsonSerializer.Serialize<object>(response);
                JsonNode jsonNode = JsonNode.Parse(jsonString)!;
                JsonNode itemsNode = jsonNode!["items"]!;

                foreach (var item in itemsNode.AsArray())
                {
                    var status = JsonSerializer.Deserialize<SparkApplicationStatus>(item!["status"]!, _options);
                    var applicationState = status?.ApplicationState["state"];

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

                return sparkApplications.AsEnumerable();
            });
        }

        public async Task<int> DeleteFailedSparkApplicationsAsync(string keepDate = null)
        {
            try
            {
                var date = DateTime.Parse(HttpUtility.UrlDecode(keepDate));
                var crs = await GetSparkApplicationsAsync("failed");
                var deleted = 0;
                foreach (var cr in crs)
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
                return deleted;
            }
            catch
            {
                return -1;
            }
        }
    }
}
