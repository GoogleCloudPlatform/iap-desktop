using Google.Solutions.CloudIap.Plugin.Integration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.CloudIap.Plugin.Gui
{
    internal static class GithubAdapter
    {
        private const string LatestReleaseUrl = "https://api.github.com/repos/GoogleCloudPlatform/iap-windows-rdc-plugin/releases/latest";

        public static async Task<Release> FindLatestReleaseAsync(
            CancellationToken cancellationToken)
        {
            var assemblyName = typeof(ComputeEngineAdapter).Assembly.GetName();
            var client = new RestClient($"{assemblyName.Name}/{assemblyName.Version}");

            var latestRelease = await client.GetAsync<Release>(
                LatestReleaseUrl,
                cancellationToken).ConfigureAwait(false);
            if (latestRelease == null)
            {
                return null;
            }
            else
            {
                // New release available.
                return latestRelease;
            }
        }

        internal class Release
        {
            [JsonProperty("tag_name")]
            public string TagName { get; set; }

            public Version TagVersion => Version.Parse(this.TagName);

            [JsonProperty("html_url")]
            public string HtmlUrl { get; set; }

            [JsonProperty("assets")]
            public List<ReleaseAsset> Assets { get; set; }
        }

        internal class ReleaseAsset
        {
            [JsonProperty("browser_download_url")]
            public string DownloadUrl { get; set; }
        }
    }
}
