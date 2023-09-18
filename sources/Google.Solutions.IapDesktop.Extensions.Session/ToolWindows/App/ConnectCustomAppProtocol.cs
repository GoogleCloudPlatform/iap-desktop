using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.Platform.Dispatch;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.App
{
    internal class ConnectCustomAppProtocol : ConnectAppProtocolCommandBase
    {
        private readonly IIapTransportFactory transportFactory;
        private readonly IWin32ProcessFactory processFactory;

        public ConnectCustomAppProtocol(
            string text,
            IIapTransportFactory transportFactory,
            IWin32ProcessFactory processFactory, 
            IJobService jobService) 
            : base(text, jobService)
        {
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

            // TODO: input dialog

            ushort port = 123;

            //
            // Create an ephermeral protocol and context, bypassing
            // the usual factory.
            //
            var protocol = new AppProtocol(
                $"Ephemeral (port {port})",
                Array.Empty<ITrait>(),
                port,
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
