using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Services.Integration;
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
using System;
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
        private readonly IJobService jobService;
        private readonly ProtocolRegistry protocolRegistry;
        private readonly IIapTransportFactory transportFactory;
        private readonly IWin32ProcessFactory processFactory;
        private readonly IConnectionSettingsService settingsService;
        private readonly ICredentialDialog credentialDialog;

        public AppCommands(
            IWin32Window ownerWindow,
            IJobService jobService,
            ProtocolRegistry protocolRegistry,
            IIapTransportFactory transportFactory,
            IWin32ProcessFactory processFactory,
            IConnectionSettingsService settingsService,
            ICredentialDialog credentialDialog)
        {
            this.ownerWindow = ownerWindow.ExpectNotNull(nameof(ownerWindow));
            this.jobService = jobService.ExpectNotNull(nameof(jobService));
            this.protocolRegistry = protocolRegistry.ExpectNotNull(nameof(protocolRegistry));
            this.transportFactory = transportFactory.ExpectNotNull(nameof(transportFactory));
            this.processFactory = processFactory.ExpectNotNull(nameof(processFactory));
            this.settingsService = settingsService.ExpectNotNull(nameof(settingsService));
            this.credentialDialog = credentialDialog.ExpectNotNull(nameof(credentialDialog));

            this.ConnectWithContextCommand = new ConnectWithAppCommand();
        }

        //---------------------------------------------------------------------
        // Context commands.
        //---------------------------------------------------------------------

        public IContextCommand<IProjectModelNode> ConnectWithContextCommand { get; }

        public IEnumerable<IContextCommand<IProjectModelNode>> ConnectWithAppCommands
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
                        protocol.Name,
                        this.ownerWindow,
                        this.jobService,
                        factory,
                        this.credentialDialog);
                }
            }
        }

        //---------------------------------------------------------------------
        // Command classes.
        //---------------------------------------------------------------------

        private class ConnectWithAppCommand : MenuCommandBase<IProjectModelNode>
        {
            public ConnectWithAppCommand() 
                : base("Connect &with")
            {
            }

            protected override bool IsAvailable(IProjectModelNode context)
            {
                return context is IProjectModelInstanceNode;
            }

            protected override bool IsEnabled(IProjectModelNode context)
            {
                return ((IProjectModelInstanceNode)context).IsRunning;
            }
        }

        private class OpenWithClientCommand : MenuCommandBase<IProjectModelNode>  //TODO: Add test
        {
            private readonly IWin32Window ownerWindow;
            private readonly IJobService jobService;
            private readonly ICredentialDialog credentialDialog;
            private readonly AppContextFactory contextFactory;

            public OpenWithClientCommand(
                string name,
                IWin32Window ownerWindow,
                IJobService jobService,
                AppContextFactory contextFactory,
                ICredentialDialog credentialDialog) 
                : base($"&{name}")
            {
                this.ownerWindow = ownerWindow.ExpectNotNull(nameof(ownerWindow));
                this.jobService = jobService.ExpectNotNull(nameof(jobService));
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

                //
                // Check which credential we need to use.
                //
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

                //
                // Connect a transport. This can take a bit, so do it in a job.
                //
                var transport = await this.jobService
                    .RunInBackground(
                        new JobDescription(
                            $"Connecting to {instance.Instance.Name}...",
                            JobUserFeedbackType.BackgroundFeedback),
                        cancellationToken => context.ConnectTransportAsync(cancellationToken))
                    .ConfigureAwait(false);

                if (context.CanLaunchClient)
                {
                    var process = context.LaunchClient(transport);

                    // TODO: Add process to job.
                    process.Resume();

                    //
                    // Client app launched successfully. Keep the transport
                    // open until the app is closed, but don't await.
                    //
                    _ = process
                        .WaitAsync(TimeSpan.MaxValue)
                        .ContinueWith(t =>
                        {
                            transport.Dispose();
                            process.Dispose();

                            return t.Result;
                        });
                }
                else
                {
                    throw new NotImplementedException("Client cannot be launched");
                }
            }
        }
    }
}
