using Google.Solutions.Compute;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Services.Workflows
{
    public class CredentialsService
    {
        private readonly IJobService jobService;
        private readonly IAuthorizationAdapter authService;
        private readonly IComputeEngineAdapter computeEngineAdapter;

        public CredentialsService(IServiceProvider serviceProvider)
        {
            this.jobService = serviceProvider.GetService<IJobService>();
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
    }
}
