using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CliWrap;
using KubeStatus.Models;
using Microsoft.AspNetCore.Http;
using Prometheus;

namespace KubeStatus.Data
{
    public class HelmService(IHttpContextAccessor httpContextAccessor)
    {
        private readonly Counter _helmRollback = Metrics.CreateCounter(
            "kube_status_helm_rollback_total",
            "Number of rollbacks per helm release.",
            new CounterConfiguration
            {
                LabelNames = ["User", "Namespace", "Release"]
            });

        private readonly Counter _helmUninstall = Metrics.CreateCounter(
            "kube_status_helm_uninstall_total",
            "Number of uninstalls per helm release.",
            new CounterConfiguration
            {
                LabelNames = ["User", "Namespace", "Release"]
            });

        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        public async Task<IEnumerable<HelmListItem>> HelmListAll(string k8sNamespace = "default")
        {
            List<string> cliArgs = GetHelmCliArguments();
            var stdOutBuffer = new StringBuilder();

            Command cmd = Cli.Wrap("helm")
                .WithArguments(args => args
                    .Add("list")
                    .Add("--all")
                    .Add("--namespace")
                    .Add(k8sNamespace)
                    .Add("--no-headers")
                    .Add(cliArgs)
                ) | stdOutBuffer;

            CommandResult result = await cmd
                .ExecuteAsync();

            return ParseListItems(stdOutBuffer);
        }

        public async Task<string> HelmRollback(string package, string k8sNamespace = "default")
        {
            List<string> cliArgs = GetHelmCliArguments();
            var stdOutBuffer = new StringBuilder();

            Command cmd = Cli.Wrap("helm")
                .WithArguments(args => args
                    .Add("rollback")
                    .Add(package)
                    .Add("--namespace")
                    .Add(k8sNamespace)
                    .Add(cliArgs)
                ) | stdOutBuffer;

            CommandResult result = await cmd
                .ExecuteAsync();

            _helmRollback.WithLabels(_httpContextAccessor.GetUserIdentityName(), k8sNamespace, package).Inc();

            return stdOutBuffer.ToString();
        }

        public async Task<string> HelmUninstall(string package, string k8sNamespace = "default")
        {
            List<string> cliArgs = GetHelmCliArguments();
            var stdOutBuffer = new StringBuilder();

            Command cmd = Cli.Wrap("helm")
                .WithArguments(args => args
                    .Add("uninstall")
                    .Add(package)
                    .Add("--namespace")
                    .Add(k8sNamespace)
                    .Add(cliArgs)
                ) | stdOutBuffer;

            CommandResult result = await cmd
                .ExecuteAsync();

            _helmUninstall.WithLabels(_httpContextAccessor.GetUserIdentityName(), k8sNamespace, package).Inc();

            return stdOutBuffer.ToString();
        }

        private List<string> GetHelmCliArguments()
        {
            k8s.KubernetesClientConfiguration config = Helper.GetKubernetesClientConfiguration();
            List<string> cliArgs = ["--kube-apiserver", config.Host];

            if (!string.IsNullOrWhiteSpace(config.AccessToken))
            {
                cliArgs.Add("--kube-token");
                cliArgs.Add(config.AccessToken);
            }
            else if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("KUBE_TOKEN_FILE") ?? string.Empty))
            {
                string token = File.ReadAllText(Environment.GetEnvironmentVariable("KUBE_TOKEN_FILE") ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    cliArgs.Add("--kube-token");
                    cliArgs.Add(token);
                }
            }

            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("KUBE_CA_FILE")))
            {
                cliArgs.Add("--kube-insecure-skip-tls-verify");
            }
            else
            {
                cliArgs.Add("--kube-ca-file");
                cliArgs.Add(Environment.GetEnvironmentVariable("KUBE_CA_FILE") ?? string.Empty);
            }

            return cliArgs;
        }

        private IEnumerable<HelmListItem> ParseListItems(StringBuilder result)
        {
            var items = new List<HelmListItem>();
            string[] lines = result.ToString().Split(Environment.NewLine.ToCharArray());

            foreach (string line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    string[] values = line.Split('\t');

                    int revision = int.TryParse(values[2].Trim(), out revision) ? revision : 1;

                    string[] dateParts = values[3].Split(' ');
                    _ = DateTimeOffset.TryParse($"{dateParts[0]} {dateParts[1]} {dateParts[2]}",
                        null,
                        DateTimeStyles.AdjustToUniversal,
                        out DateTimeOffset updated);

                    items.Add(new HelmListItem
                    {
                        Name = values[0].Trim(),
                        Namespace = values[1].Trim(),
                        Revision = revision,
                        Updated = updated,
                        Status = values[4].Trim(),
                        Chart = values[5].Trim(),
                        AppVersion = values[6].Trim()
                    });
                }
            }

            return items;
        }
    }
}
