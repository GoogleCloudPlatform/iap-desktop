using Google.Solutions.Compute;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Util;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Services.Workflows
{
    public class CredentialsService
    {
        private readonly IJobService jobService;
        private readonly IEventService eventService;
        private readonly IAuthorizationAdapter authService;
        private readonly IComputeEngineAdapter computeEngineAdapter;

        public CredentialsService(IServiceProvider serviceProvider)
        {
            this.jobService = serviceProvider.GetService<IJobService>();
            this.eventService = serviceProvider.GetService<IEventService>();
            this.authService = serviceProvider.GetService<IAuthorizationAdapter>();
            this.computeEngineAdapter = serviceProvider.GetService<IComputeEngineAdapter>();
        }

        public async Task<NetworkCredential> GenerateCredentials(
            IWin32Window owner,
            VmInstanceReference instanceRef)
        {
            var suggestedUsername = this.authService.Authorization.SuggestWindowsUsername();

            // Prompt for username to use.
            var username = new GenerateCredentialsDialog().PromptForUsername(owner, suggestedUsername);
            if (username == null)
            {
                return null;
            }

            var credentials = await this.jobService.RunInBackground(
                new JobDescription("Generating Windows logon credentials..."),
                token =>
                {
                    return this.computeEngineAdapter.ResetWindowsUserAsync(instanceRef, username, token);
                });

            new ShowCredentialsDialog().ShowDialog(
                owner,
                credentials.UserName,
                credentials.Password);

            return credentials;
        }

        internal async Task<NetworkCredential> GenerateAndSaveCredentials(
            IWin32Window owner, 
            VmInstanceNode vmNode)
        {
            var credentials = await GenerateCredentials(owner, vmNode.Reference);
            if (credentials == null)
            {
                // Aborted.
                return null;
            }

            // Update node to persist settings.
            vmNode.Username = credentials.UserName;
            vmNode.CleartextPassword = credentials.Password;
            vmNode.Domain = null;
            vmNode.SaveChanges();

            // Fire an event to update anybody using the node.
            await this.eventService.FireAsync(new ProjectExplorerNodeSelectedEvent(vmNode));

            return credentials;
        }
    }
}
