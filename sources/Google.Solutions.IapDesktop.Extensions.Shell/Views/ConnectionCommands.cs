using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Rdp;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views
{
    [Service]
    public class ConnectionCommands
    {
        public ConnectionCommands(
            UrlCommands urlCommands,
            Service<IRdpConnectionService> connectionService)
        {
            //
            // Install command for launching URLs.
            //
            urlCommands.LaunchRdpUrl = new LaunchRdpUrlCommand(connectionService);
        }

        //---------------------------------------------------------------------
        // Commands classes
        //---------------------------------------------------------------------

        private class LaunchRdpUrlCommand : ToolContextCommand<IapRdpUrl>
        {
            private readonly Service<IRdpConnectionService> connectionService;

            public LaunchRdpUrlCommand(
                Service<IRdpConnectionService> connectionService)
                : base("Launch &RDP URL")
            {
                this.connectionService = connectionService;
            }

            protected override bool IsAvailable(IapRdpUrl url)
            {
                return true;
            }

            protected override bool IsEnabled(IapRdpUrl url)
            {
                return true;
            }

            public override Task ExecuteAsync(IapRdpUrl url)
            {
                return this.connectionService
                    .GetInstance()
                    .ActivateOrConnectInstanceAsync(url);
            }
        }
    }
}
