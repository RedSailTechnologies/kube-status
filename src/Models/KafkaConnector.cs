namespace KubeStatus.Models
{
    public class KafkaConnector
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string ConnectorState { get; set; }
        public string TaskState { get; set; }
        public string TaskTrace { get; set; }
    }
}
