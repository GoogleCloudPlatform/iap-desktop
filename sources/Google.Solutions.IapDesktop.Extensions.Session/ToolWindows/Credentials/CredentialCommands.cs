using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Properties;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.Mvvm.Binding.Commands;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Credentials
{
    [Service]
    public class CredentialCommands
    {
        public CredentialCommands(
            IWin32Window window,
            IConnectionSettingsService settingsService,
            ICreateCredentialsWorkflow workflow)
        {
            this.ContextMenuNewCredentials = new NewCredentialsCommand(
                window,
                settingsService,
                workflow)
            {
                CommandType = MenuCommandType.MenuCommand
            };
            this.ToolbarNewCredentials = new NewCredentialsCommand(
                window,
                settingsService,
                workflow)
            {
                CommandType = MenuCommandType.MenuCommand
            };
        }

        //---------------------------------------------------------------------
        // Context commands.
        //---------------------------------------------------------------------

        public IContextCommand<IProjectModelNode> ContextMenuNewCredentials { get; }
        public IContextCommand<IProjectModelNode> ToolbarNewCredentials { get; }

        private class NewCredentialsCommand : MenuCommandBase<IProjectModelNode> //TODO: Test
        {
            private readonly IWin32Window window;
            private readonly IConnectionSettingsService settingsService;
            private readonly ICreateCredentialsWorkflow workflow;

            public NewCredentialsCommand(
                IWin32Window window,
                IConnectionSettingsService settingsService,
                ICreateCredentialsWorkflow workflow) 
                : base("New logon &credentials...")
            {
                this.window = window;
                this.settingsService = settingsService;
                this.workflow = workflow;

                this.Image = Resources.AddCredentials_16;
                this.ActivityText = "Generating new Windows logon credentials";
            }

            public override string Id => "GenerateWindowsCredentials";

            protected override bool IsAvailable(IProjectModelNode node)
            {
                return node is IProjectModelInstanceNode instanceNode &&
                    instanceNode.IsWindowsInstance();
            }

            protected override bool IsEnabled(IProjectModelNode node)
            {
                return node is IProjectModelInstanceNode instanceNode &&
                   instanceNode.IsRunning;
            }

            public async override Task ExecuteAsync(IProjectModelNode node)
            {
                Debug.Assert(IsAvailable(node));
                Debug.Assert(IsEnabled(node));

                var instanceNode = (IProjectModelInstanceNode)node;

                var settings = this.settingsService.GetConnectionSettings(node);

                await this.workflow
                    .CreateCredentialsAsync(
                        this.window,
                        instanceNode.Instance,
                        settings.TypedCollection,
                        false)
                    .ConfigureAwait(true);

                settings.Save();
            }
        }
    }
}
