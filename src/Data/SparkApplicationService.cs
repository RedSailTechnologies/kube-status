using System.Collections.Generic;
using System.Threading.Tasks;
using k8s;
using KubeStatus.Models;
using Newtonsoft.Json.Linq;

namespace KubeStatus.Data
{
    public class SparkApplicationService
    {
        public async Task<IEnumerable<SparkApplication>> GetAllSparkApplicationsAsync()
        {
            return await Task.FromResult(GetSparkApplications());
        }

        private IEnumerable<SparkApplication> GetSparkApplications()
        {
            var sparkApplications = new List<SparkApplication>();

            var client = Helper.GetKubernetesClient();

            var clusterCustomObjects = ((JObject)client.ListClusterCustomObject(Helper.SparkGroup(), Helper.SparkApplicationVersion(), Helper.SparkApplicationPlural())).SelectToken("items").Children();

            foreach (var clusterCustomObject in clusterCustomObjects)
            {
                var apiVersion = clusterCustomObject.SelectToken("apiVersion").ToString();
                var kind = clusterCustomObject.SelectToken("kind").ToString();
                var metadata = clusterCustomObject.SelectToken("metadata").ToObject<Metadata>();
                var status = clusterCustomObject.SelectToken("status").ToObject<SparkApplicationStatus>();
                var spec = clusterCustomObject.SelectToken("spec").ToString();

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
