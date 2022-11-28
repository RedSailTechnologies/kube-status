using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using CliWrap;
using KubeStatus.Models;

namespace KubeStatus.Data
{
    public class HelmService
    {
        public async Task<IEnumerable<HelmListItem>> GetHelmListAll(string k8sNamespace = "default")
        {
            var config = Helper.GetKubernetesClientConfiguration();

            Console.WriteLine(config.ToYaml());

            var stdOutBuffer = new StringBuilder();

            List<string> accessToken = new List<string>(); ;
            if (!string.IsNullOrWhiteSpace(config.AccessToken))
            {
                accessToken.Add("--kube-token");
                accessToken.Add(config.AccessToken);
            }

            List<string> helmCaOrBypass = new List<string>(); ;
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("KUBE_CA_FILE")))
            {
                helmCaOrBypass.Add("--kube-insecure-skip-tls-verify");
            }
            else
            {
                helmCaOrBypass.Add("--kube-ca-file");
                helmCaOrBypass.Add(Environment.GetEnvironmentVariable("KUBE_CA_FILE"));
            }

            var cmd = Cli.Wrap("helm")
                .WithArguments(args => args
                    .Add("list")
                    .Add("--all")
                    .Add("--namespace")
                    .Add(k8sNamespace)
                    .Add("--no-headers")
                    .Add("--kube-apiserver")
                    .Add(config.Host)
                    .Add(accessToken)
                    .Add(helmCaOrBypass)
                ) | stdOutBuffer;
            
            Console.WriteLine(cmd.ToString());

            var result = await cmd
                .ExecuteAsync();

            return ParseListItems(stdOutBuffer);
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
