using Google.Solutions.Compute;
using System.Diagnostics;

namespace Google.Solutions.IapDesktop.Application.Services
{
    public class CloudConsoleService
    {
        private void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo()
            {
                UseShellExecute = true,
                Verb = "open",
                FileName = url
            }); ;
        }

        public void OpenVmInstance(VmInstanceReference instance)
        {
            OpenUrl("https://console.cloud.google.com/compute/instancesDetail/zones/" +
                    $"{instance.Zone}/instances/{instance.InstanceName}?project={instance.ProjectId}");
        }

        public void OpenVmInstanceLogs(VmInstanceReference instance, ulong instanceId)
        {
            OpenUrl("https://console.cloud.google.com/logs/viewer?" +
                   $"resource=gce_instance%2Finstance_id%2F{instanceId}&project={instance.ProjectId}");
        }

        public void OpenIapOverviewDocs()
        {
            OpenUrl("https://cloud.google.com/iap/docs/tcp-forwarding-overview");
        }

        public void OpenIapAccessDocs()
        {
            OpenUrl("https://cloud.google.com/iap/docs/using-tcp-forwarding");
        }
        public void ConfigureIapAccess(string projectId)
        {
            OpenUrl($"https://console.cloud.google.com/security/iap?project={projectId}");
        }
    }
}
