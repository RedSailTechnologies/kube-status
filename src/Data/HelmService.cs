using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using CliWrap;
using Prometheus;

using KubeStatus.Models;

namespace KubeStatus.Data
{
    public class HelmService
    {
        private readonly Counter _helmRollback = Metrics.CreateCounter(
            "kube_status_helm_rollback_total",
            "Number of rollbacks per helm release.",
            new CounterConfiguration
            {
                LabelNames = new[] { "User", "Namespace", "Release" }
            });

        private readonly Counter _helmUninstall = Metrics.CreateCounter(
            "kube_status_helm_uninstall_total",
            "Number of uninstalls per helm release.",
            new CounterConfiguration
            {
                LabelNames = new[] { "User", "Namespace", "Release" }
            });

        public async Task<IEnumerable<HelmListItem>> HelmListAll(string k8sNamespace = "default")
        {
            List<string> cliArgs = GetHelmCliArguments();
            var stdOutBuffer = new StringBuilder();

            var cmd = Cli.Wrap("helm")
                .WithArguments(args => args
                    .Add("list")
                    .Add("--all")
                    .Add("--namespace")
                    .Add(k8sNamespace)
                    .Add("--no-headers")
                    .Add(cliArgs)
                ) | stdOutBuffer;

            var result = await cmd
                .ExecuteAsync();

            return ParseListItems(stdOutBuffer);
        }

        public async Task<string> HelmRollback(string package, string k8sNamespace = "default")
        {
            List<string> cliArgs = GetHelmCliArguments();
            var stdOutBuffer = new StringBuilder();

            var cmd = Cli.Wrap("helm")
                .WithArguments(args => args
                    .Add("rollback")
                    .Add(package)
                    .Add("--namespace")
                    .Add(k8sNamespace)
                    .Add(cliArgs)
                ) | stdOutBuffer;

            var result = await cmd
                .ExecuteAsync();

            _helmRollback.WithLabels(Environment.UserName ?? "", k8sNamespace, package).Inc();

            return stdOutBuffer.ToString();
        }

        public async Task<string> HelmUninstall(string package, string k8sNamespace = "default")
        {
            List<string> cliArgs = GetHelmCliArguments();
            var stdOutBuffer = new StringBuilder();

            var cmd = Cli.Wrap("helm")
                .WithArguments(args => args
                    .Add("uninstall")
                    .Add(package)
                    .Add("--namespace")
                    .Add(k8sNamespace)
                    .Add(cliArgs)
                ) | stdOutBuffer;

            var result = await cmd
                .ExecuteAsync();

            _helmUninstall.WithLabels(Environment.UserName ?? "", k8sNamespace, package).Inc();

            return stdOutBuffer.ToString();
        }

        private List<string> GetHelmCliArguments()
        {
            var config = Helper.GetKubernetesClientConfiguration();
            List<string> cliArgs = new List<string>();

            cliArgs.Add("--kube-apiserver");
            cliArgs.Add(config.Host);

            if (!string.IsNullOrWhiteSpace(config.AccessToken))
            {
                cliArgs.Add("--kube-token");
                cliArgs.Add(config.AccessToken);
            }
            else if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("KUBE_TOKEN_FILE")))
            {
                var token = File.ReadAllText(Environment.GetEnvironmentVariable("KUBE_TOKEN_FILE"));
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
                cliArgs.Add(Environment.GetEnvironmentVariable("KUBE_CA_FILE"));
            }

            return cliArgs;
        }

        private IEnumerable<HelmListItem> ParseListItems(StringBuilder result)
        {
            var items = new List<HelmListItem>();
            string[] lines = result.ToString().Split(Environment.NewLine.ToCharArray());

            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    string[] values = line.Split('\t');

                    int revision = int.TryParse(values[2].Trim(), out revision) ? revision : 1;

                    string[] dateParts = values[3].Split(' ');
                    DateTimeOffset.TryParse($"{dateParts[0]} {dateParts[1]} {dateParts[2]}",
                        null as IFormatProvider,
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
