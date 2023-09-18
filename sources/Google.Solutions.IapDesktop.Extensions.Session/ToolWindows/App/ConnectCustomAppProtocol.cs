using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.Platform.Dispatch;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.App
{
    internal class ConnectCustomAppProtocol : ConnectAppProtocolCommandBase
    {
        private readonly IWin32Window ownerWindow;
        private readonly IInputDialog inputDialog;
        private readonly IIapTransportFactory transportFactory;
        private readonly IWin32ProcessFactory processFactory;
        
        public ConnectCustomAppProtocol(
            IWin32Window ownerWindow,
            string text,
            IIapTransportFactory transportFactory,
            IWin32ProcessFactory processFactory, 
            IJobService jobService,
            IInputDialog inputDialog,
            INotifyDialog notifyDialog) 
            : base(text, jobService, notifyDialog)
        {
            this.ownerWindow = ownerWindow.ExpectNotNull(nameof(ownerWindow));
            this.inputDialog = inputDialog.ExpectNotNull(nameof(inputDialog));
            this.transportFactory = transportFactory.ExpectNotNull(nameof(transportFactory));
            this.processFactory = processFactory.ExpectNotNull(nameof(processFactory));
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        public override string Id
        {
            get => GetType().Name;
        }

        protected override bool IsEnabled(IProjectModelNode context)
        {
            return true;
        }

        protected internal override Task<AppProtocolContext> CreateContextAsync( // TODO: test
            IProjectModelInstanceNode instance, 
            CancellationToken cancellationToken)
        {
            //
            // We don't have a protocol, so we need to create on on the
            // fly based on user input.
            //
            void validatePortNumber(
                string input,
                out bool valid,
                out string warning)
            {
                valid = ushort.TryParse(input, out var portNumber) && portNumber > 0;
                warning = string.IsNullOrEmpty(input) || valid
                    ? string.Empty
                    : $"Enter a port number between 1 and {ushort.MaxValue}";
            }

            if (this.inputDialog.Prompt(
                    this.ownerWindow,
                    new InputDialogParameters()
                    {
                        Title = $"Connect to {instance.DisplayName}",
                        Caption = "Forward local port",
                        Message = $"Allocate a local port and forward it to {instance.DisplayName}",
                        Cue = "Remote port number",
                        Validate = validatePortNumber
                    },
                    out var remotePortString) != DialogResult.OK ||
                !ushort.TryParse(remotePortString, out var remotePort))
            {
                throw new TaskCanceledException();
            }

            //
            // Create an ephemeral protocol and context, bypassing
            // the usual factory.
            //
            var protocol = new AppProtocol(
                $"Ephemeral (port {remotePort})",
                Array.Empty<ITrait>(),
                remotePort,
                null,
                null);

            var context = new AppProtocolContext(
                protocol,
                this.transportFactory,
                this.processFactory,
                instance.Instance);

            return Task.FromResult(context);
        }
    }
}
