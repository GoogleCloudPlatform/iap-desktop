using Google.Solutions.IapDesktop.Application.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Adapters
{
    public class GithubAdapter
    {
        private const string LatestReleaseUrl = "https://api.github.com/repos/GoogleCloudPlatform/iap-windows-rdc-plugin/releases/latest";
        public const string BaseUrl = "https://github.com/GoogleCloudPlatform/iap-windows-rdc-plugin";

        private void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo()
            {
                UseShellExecute = true,
                Verb = "open",
                FileName = url
            });
        }

        public void ReportIssue()
        {
            var version = typeof(GithubAdapter).Assembly.GetName().Version;
            var body = "Expected behavior:\n" +
                       "* Step 1\n" +
                       "* Step 2\n" +
                       "* ...\n" +
                       "\n" +
                       "Observed behavior:\n" +
                       "* Step 1\n" +
                       "* Step 2\n" +
                       "* ...\n" +
                       "\n" +
                       $"Installed version: {version}\n" +
                       $".NET Version: {Environment.Version}\n" +
                       $"OS Version: {Environment.OSVersion}";
            OpenUrl($"{BaseUrl}/issues/new?body={WebUtility.UrlEncode(body)}");
        }

        public async Task<Release> FindLatestReleaseAsync(CancellationToken cancellationToken)
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

        public class Release
        {
            [JsonProperty("tag_name")]
            public string TagName { get; set; }

            public Version TagVersion => Version.Parse(this.TagName);

            [JsonProperty("html_url")]
            public string HtmlUrl { get; set; }

            [JsonProperty("assets")]
            public List<ReleaseAsset> Assets { get; set; }
        }

        public class ReleaseAsset
        {
            [JsonProperty("browser_download_url")]
            public string DownloadUrl { get; set; }
        }
    }
}
