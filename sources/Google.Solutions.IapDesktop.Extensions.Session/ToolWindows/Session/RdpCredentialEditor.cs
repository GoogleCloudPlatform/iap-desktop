using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Credentials;
using Google.Solutions.Settings;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session
{
    /// <summary>
    /// Lets users edit or amend RDP credentials used in 
    /// connection settings.
    /// </summary>
    public interface IRdpCredentialEditor
    {
        /// <summary>
        /// Check if changes are allowed to be saved.
        /// </summary>
        public bool AllowSave { get; }

        /// <summary>
        /// Generate new credentials and update connection settings.
        /// </summary>
        /// <exception cref="OperationCanceledException">when cancelled by user</exception>
        Task ReplaceCredentialsAsync(bool silent);

        /// <summary>
        /// Amend existing credentials if they are incomplete.
        /// </summary>
        /// <exception cref="OperationCanceledException">when cancelled by user</exception>
        Task AmendCredentialsAsync(
            RdpCredentialGenerationBehavior generationBehavior);
    }

    public class RdpCredentialEditor : IRdpCredentialEditor
    {
        private readonly IWin32Window? owner;
        private readonly IAuthorization authorization;
        private readonly IJobService jobService;
        private readonly IWindowsCredentialGenerator credentialGenerator;
        private readonly IDialogFactory<NewCredentialsView, NewCredentialsViewModel> newCredentialFactory;
        private readonly IDialogFactory<ShowCredentialsView, ShowCredentialsViewModel> showCredentialFactory;

        internal RdpCredentialEditor(
            IWin32Window? owner,
            Settings.ConnectionSettings settings,
            IAuthorization authorization,
            IJobService jobService,
            IWindowsCredentialGenerator credentialGenerator,
            IDialogFactory<NewCredentialsView, NewCredentialsViewModel> newCredentialFactory,
            IDialogFactory<ShowCredentialsView, ShowCredentialsViewModel> showCredentialFactory)
        {
            this.owner = owner;
            this.Settings = settings;

            this.authorization = authorization;
            this.jobService = jobService;
            this.credentialGenerator = credentialGenerator;
            this.newCredentialFactory = newCredentialFactory;
            this.showCredentialFactory = showCredentialFactory;

            Debug.Assert(settings.Resource is InstanceLocator);
        }

        /// <summary>
        /// Settings that are being editied.
        /// </summary>
        public Settings.ConnectionSettings Settings { get; }

        /// <summary>
        /// Instance for which settings are being edited.
        /// </summary>
        public InstanceLocator Instance
        {
            get => (InstanceLocator)this.Settings.Resource;
        }

        internal bool AreCredentialsIncomplete
        {
            get => 
                string.IsNullOrEmpty(this.Settings.RdpUsername.Value) ||
                string.IsNullOrEmpty(this.Settings.RdpPassword.GetClearTextValue());
        }

        internal async Task<NetworkCredential> CreateCredentialsAsync(
            IWin32Window? owner,
            InstanceLocator instanceLocator,
            string? username,
            bool silent)
        {
            if (username == null ||
                string.IsNullOrEmpty(username) ||
                !WindowsUser.IsLocalUsername(username))
            {
                username = WindowsUser.SuggestUsername(this.authorization.Session);
            }

            if (!silent)
            {
                //
                // Prompt user to customize the defaults.
                //
                using (var dialog = this.newCredentialFactory.CreateDialog())
                {
                    dialog.ViewModel.Username = username;
                    if (dialog.ShowDialog(owner) == DialogResult.OK)
                    {
                        username = dialog.ViewModel.Username;
                    }
                    else
                    {
                        throw new OperationCanceledException();
                    }
                }
            }

            var credentials = await this.jobService.RunAsync(
                new JobDescription("Generating Windows logon credentials..."),
                token => this.credentialGenerator
                    .CreateWindowsCredentialsAsync(
                        instanceLocator,
                        username,
                        UserFlags.AddToAdministrators,
                        token))
                    .ConfigureAwait(true);

            if (!silent)
            {
                using (var dialog = this.showCredentialFactory.CreateDialog(
                    new ShowCredentialsViewModel(
                        credentials.UserName,
                        credentials.Password)))
                {
                    dialog.ShowDialog(owner);
                }
            }

            return credentials;
        }

        //---------------------------------------------------------------------
        // IRdpCredentialEditor.
        //---------------------------------------------------------------------

        public bool AllowSave { get; private set; } = true;

        /// <summary>
        /// Create new credentials and use them to replace 
        /// current credentials (if any).
        /// </summary>
        /// <exception cref="OperationCanceledException">when cancelled</exception>
        public async Task ReplaceCredentialsAsync(bool silent)
        {
            var credentials = await CreateCredentialsAsync(
                owner,
                this.Instance,
                this.Settings.RdpUsername.Value,
                silent);

            //
            // Save credentials.
            //
            this.Settings.RdpUsername.Value = credentials.UserName;
            this.Settings.RdpPassword.SetClearTextValue(credentials.Password);

            //
            // NB. The computer might be joined to a domain, therefore force a local logon.
            //
            this.Settings.RdpDomain.Value = ".";
        }

        /// <summary>
        /// Allow user to amend or replace the current credentials
        /// if they're incomplete.
        /// </summary>
        /// <exception cref="OperationCanceledException">when cancelled</exception>
        public async Task AmendCredentialsAsync(
            RdpCredentialGenerationBehavior generationBehavior)
        { 
            if (this.Settings.RdpNetworkLevelAuthentication.Value 
                == RdpNetworkLevelAuthentication.Disabled)
            {
                //
                // When NLA is disabled, RDP credentials don't matter.
                //
                return;
            }

            await Task.Yield();

            //TODO: Port logic from SelectCredentialsDialog
            throw new NotImplementedException(); 
        }
    }
}
