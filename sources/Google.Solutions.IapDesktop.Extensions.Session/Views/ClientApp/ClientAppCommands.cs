using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.ClientApp;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Platform.Dispatch;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.Views.ClientApp
{
    [Service]
    public class ClientAppCommands
    {
        //---------------------------------------------------------------------
        // Context commands.
        //---------------------------------------------------------------------

        public IReadOnlyCollection<IContextCommand<IProjectModelNode>> OpenWithClient { get; }

        //---------------------------------------------------------------------
        // Command classes.
        //---------------------------------------------------------------------

        private class OpenWithClientCommand : MenuCommandBase<IProjectModelNode>
        {
            private readonly IWin32Window ownerWindow;
            private readonly ICredentialDialog credentialDialog;
            private readonly ClientAppContextFactory contextFactory;

            public OpenWithClientCommand(
                IWin32Window ownerWindow,
                string name,
                ClientAppContextFactory contextFactory,
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
                return this.contextFactory.Protocol.Client.IsAvailable;
            }

            public override async Task ExecuteAsync(IProjectModelNode node)
            {
                var instance = (IProjectModelInstanceNode)node;

                var requiredCredential = NetworkCredentialType.Default;
                var client = this.contextFactory.Protocol.Client;
                if (client is IWindowsClientApp windowsClient)
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
