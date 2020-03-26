using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Adapters
{
    public class GithubAdapter
    {
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
    }
}
