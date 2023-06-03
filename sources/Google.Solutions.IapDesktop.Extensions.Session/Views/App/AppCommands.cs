using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.App;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Platform.Dispatch;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.Views.App
{
    [Service]
    public class AppCommands
    {
        private readonly IWin32Window ownerWindow;
        private readonly ProtocolRegistry protocolRegistry;
        private readonly IIapTransportFactory transportFactory;
        private readonly IWin32ProcessFactory processFactory;
        private readonly IConnectionSettingsService settingsService;
        private readonly ICredentialDialog credentialDialog;

        public AppCommands(
            IWin32Window ownerWindow,
            ProtocolRegistry protocolRegistry,
            IIapTransportFactory transportFactory,
            IWin32ProcessFactory processFactory,
            IConnectionSettingsService settingsService,
            ICredentialDialog credentialDialog)
        {
            this.ownerWindow = ownerWindow.ExpectNotNull(nameof(ownerWindow));
            this.protocolRegistry = protocolRegistry.ExpectNotNull(nameof(protocolRegistry));
            this.transportFactory = transportFactory.ExpectNotNull(nameof(transportFactory));
            this.processFactory = processFactory.ExpectNotNull(nameof(processFactory));
            this.settingsService = settingsService.ExpectNotNull(nameof(settingsService));
            this.credentialDialog = credentialDialog.ExpectNotNull(nameof(credentialDialog));
        }

        //---------------------------------------------------------------------
        // Context commands.
        //---------------------------------------------------------------------

        public IEnumerable<IContextCommand<IProjectModelNode>> OpenContextCommands
        {
            get
            {
                foreach (var protocol in this.protocolRegistry
                    .Protocols
                    .OfType<AppProtocol>()
                    .OrderBy(p => p.Name))
                {
                    var factory = new AppContextFactory(
                        protocol,
                        this.transportFactory,
                        this.processFactory,
                        this.settingsService);

                    yield return new OpenWithClientCommand(
                        this.ownerWindow,
                        protocol.Name,
                        factory,
                        this.credentialDialog);
                }
            }
        }

        //---------------------------------------------------------------------
        // Command classes.
        //---------------------------------------------------------------------

        private class OpenWithClientCommand : MenuCommandBase<IProjectModelNode>  //TODO: Add test
        {
            private readonly IWin32Window ownerWindow;
            private readonly ICredentialDialog credentialDialog;
            private readonly AppContextFactory contextFactory;

            public OpenWithClientCommand(
                IWin32Window ownerWindow,
                string name,
                AppContextFactory contextFactory,
                ICredentialDialog credentialDialog) 
                : base(name)
            {
                this.ownerWindow = ownerWindow.ExpectNotNull(nameof(ownerWindow));
                this.contextFactory = contextFactory.ExpectNotNull(nameof(contextFactory));
                this.credentialDialog = credentialDialog.ExpectNotNull(nameof(credentialDialog));
            }

            protected override bool IsAvailable(IProjectModelNode context)
            {
                return context is IProjectModelInstanceNode instance &&
                    this.contextFactory.Protocol.IsAvailable(instance);
            }

            protected override bool IsEnabled(IProjectModelNode context)
            {
                return true;
            }

            public override async Task ExecuteAsync(IProjectModelNode node)
            {
                var instance = (IProjectModelInstanceNode)node;

                var requiredCredential = NetworkCredentialType.Default;
                var client = this.contextFactory.Protocol.Client;
                if (client is IWindowsAppClient windowsClient)
                {
                    requiredCredential = windowsClient.RequiredCredential;
                }

                var context = (AppProtocolContext)await this.contextFactory
                    .CreateContextAsync(
                        instance,
                        requiredCredential == NetworkCredentialType.Rdp 
                            ? (uint)AppProtocolContextFlags.TryUseRdpNetworkCredentials
                            : (uint)AppProtocolContextFlags.None, 
                        CancellationToken.None)
                    .ConfigureAwait(true);

                if (requiredCredential == NetworkCredentialType.Prompt ||
                        (requiredCredential == NetworkCredentialType.Rdp && 
                        context.NetworkCredential == null))
                {
                    //
                    // Prompt for network credentials.
                    //
                    if (this.credentialDialog.PromptForWindowsCredentials(
                        this.ownerWindow,
                        this.contextFactory.Protocol.Name,
                        $"Enter credentials for {instance.DisplayName}",
                        AuthenticationPackage.Any,
                        out var credential) != DialogResult.OK)
                    {
                        //
                        // Cancelled.
                        //
                        return;
                    }

                    Debug.Assert(credential != null);
                    context.NetworkCredential = credential;
                }


                // use job! context.ConnectTransportAsync

                // context.LaunchClient()

                // wait for process, then dispose transport.
            }
        }
    }
}
