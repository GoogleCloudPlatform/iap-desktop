using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Platform.Dispatch;
using System.Collections.Generic;

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
            private readonly IWin32ProcessFactory processFactory;
            private readonly AppProtocol protocol;

            public OpenWithClientCommand(
                AppProtocol protocol,
                IWin32ProcessFactory processFactory) 
                : base("&" + protocol.Name)
            {
                this.processFactory = processFactory.ExpectNotNull(nameof(processFactory));
            }

            protected override bool IsAvailable(IProjectModelNode context)
            {
                return context is IProjectModelInstanceNode instance &&
                    this.protocol.IsAvailable(instance);
            }

            protected override bool IsEnabled(IProjectModelNode context)
            {
                return this.protocol.Client.IsAvailable;
            }

            public override void Execute(IProjectModelNode context)
            {
                if (this.protocol.Client is IWindowsProtocolClient windowsClient &&
                    windowsClient.RequiredCredential != NetworkCredentialType.Default)
                {

                }
                else
                {
                    //
                    // Launch with current user credentials.
                    //
                    
                }
            }
        }
    }
}
