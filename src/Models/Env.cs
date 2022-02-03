using Newtonsoft.Json;

namespace KubeStatus.Models
{
    public class Env
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}