using System;
using System.Collections.Generic;

namespace KubeStatus.Models
{
    public class TorDatabase
    {
        public required string Name { get; set; }
        public required string K8sNamespace { get; set; }
        public required DatabaseSpec Spec { get; set; }
        public required DatabaseStatus Status { get; set; }
    }

    public class DatabaseSpec
    {
        public string? ServiceAccount { get; set; }
        public string DbName { get; set; } = string.Empty;
        public int? Weight { get; set; } = 0;
        public List<string>? Commands { get; set; }
        public List<string>? LoopCommands { get; set; }
        public bool? UrlDecodeCommands { get; set; } = false;
        public List<string>? Schemas { get; set; }
        public bool? RevokePublicAccess { get; set; } = true;
        public int? CommandTimeout { get; set; } = 120;
        public string? RlsRole { get; set; }
        public string? VaultDefaultTtl { get; set; } = "60m";
        public string? VaultMaxTtl { get; set; } = "60m";
        public string? VaultMigrationTtl { get; set; } = "60m";
        public string? MergeSecret { get; set; }
        public string? Secret { get; set; }
        public bool? Shared { get; set; } = false;
        public bool? AlwaysReconcile { get; set; } = false;
    }

    public class DatabaseStatus
    {
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdatedDate { get; set; }
        public DateTime? LastReconciledDate { get; set; }
        public string Hash { get; set; } = string.Empty;
    }
}
