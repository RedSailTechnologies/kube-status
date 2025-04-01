using System;
using System.Collections.Generic;

namespace KubeStatus.Models
{
    public class TorEnterprise
    {
        public required string Name { get; set; }
        public required string K8sNamespace { get; set; }
        public required EnterpriseSpec Spec { get; set; }
        public required EnterpriseStatus Status { get; set; }
    }

    public class EnterpriseSpec
    {
        public string TenantId { get; set; } = string.Empty;
        public string? Host { get; set; }
        public string? Prefix { get; set; }
        public List<string>? AdditionalNamespaces { get; set; }
        public bool SkipReconcile { get; set; }
        public bool SharedOnlyDatabases { get; set; }
        public string? ServerSecret { get; set; }
        public string? Platform { get; set; } = "AzureFlex";
        public int? Port { get; set; } = 5432;
        public string? SslMode { get; set; } = "Require";
        public string? TrustServerCertificate { get; set; } = "true";
    }

    public class EnterpriseStatus
    {
        public DateTime CreatedDate { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public DateTime? LastReconciledDate { get; set; }
        public string Hash { get; set; } = string.Empty;
        public Dictionary<string, string> DatabaseHashes { get; set; } = [];
        public DateTime? LastErrorDate { get; set; }
        public string? LastErrorMessage { get; set; }
    }
}
