using Google.Apis.Auth.OAuth2.Responses;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.Windows.RemoteDesktop;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Solutions.IapDesktop.Windows;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Windows
{
    public partial class DebugWindow : ToolWindow
    {
        private readonly JobService jobService;
        private readonly IEventService eventService;
        private readonly RemoteDesktopService rdpService;
        private readonly DockPanel dockPanel;

        public DebugWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            this.jobService = serviceProvider.GetService<JobService>();
            this.eventService = serviceProvider.GetService<IEventService>();
            this.rdpService = serviceProvider.GetService<RemoteDesktopService>();

            this.TabText = this.Text;

            this.eventService.BindHandler<StatusUpdatedEvent>(
                e =>
                {
                    this.label.Text = e.Status;
                });
            this.eventService.BindAsyncHandler<StatusUpdatedEvent>(
                async e =>
                {
                    await Task.Delay(10);
                    Debug.WriteLine("Delayed in event handler");
                });


            this.dockPanel = serviceProvider.GetService<IMainForm>().MainPanel;
            
        }

        public void ShowWindow()
        {
            Show(this.dockPanel, DockState.DockRight);
        }

        public class StatusUpdatedEvent
        {
            public string Status { get; }

            public StatusUpdatedEvent(string status)
            {
                this.Status = status;
            }
        }

        private async void slowOpButton_Click(object sender, EventArgs e)
        {
            this.spinner.Visible = true;
            try
            {
                await this.jobService.RunInBackground<object>(
                    new JobDescription("This takes a while, but can be cancelled..."),
                    async token =>
                    {
                        Debug.WriteLine("Starting delay...");
                        await this.eventService.FireAsync(new StatusUpdatedEvent("Starting delay..."));

                        await Task.Delay(5000, token);

                        Debug.WriteLine("Delay over");
                        await this.eventService.FireAsync(new StatusUpdatedEvent("Done"));

                        return null;
                    });
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("Task cancelled");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
            this.spinner.Visible = false;
        }

        private async void slowNonCanelOpButton_Click(object sender, EventArgs e)
        {
            this.spinner.Visible = true;
            try
            {
                await this.jobService.RunInBackground<object>(
                    new JobDescription("This takes a while, and cannot be cancelled..."),
                    async token =>
                    {
                        Debug.WriteLine("Starting delay...");
                        await this.eventService.FireAsync(new StatusUpdatedEvent("Starting delay..."));

                        await Task.Delay(5000);

                        Debug.WriteLine("Delay over");
                        await this.eventService.FireAsync(new StatusUpdatedEvent("Done"));

                        return null;
                    });
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("Task cancelled");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
            this.spinner.Visible = false;
        }

        private async void throwExceptionButton_Click(object sender, EventArgs e)
        {
            this.spinner.Visible = true;
            try
            {
                await this.jobService.RunInBackground<object>(
                    new JobDescription("This takes a while, and cannot be cancelled..."),
                    token =>
                    {
                        throw new ApplicationException("bang!");
                    });
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("Task cancelled");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
            this.spinner.Visible = false;
        }

        private async void reauthButton_Click(object sender, EventArgs e)
        {

            this.spinner.Visible = true;
            try
            {
                await this.jobService.RunInBackground<object>(
                    new JobDescription("This takes a while, and cannot be cancelled..."),
                    token =>
                    {
                        throw new TokenResponseException(new TokenErrorResponse()
                        {
                            Error = "invalid_grant"
                        });
                    });
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("Task cancelled");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
            this.spinner.Visible = false;
        }

        private void connectButton_Click(object sender, EventArgs args)
        {
            var server = this.serverTextBox.Text.Split(':');

            try
            {
                this.rdpService.Connect(
                    server[0],
                    (ushort)(server.Length > 1 ? int.Parse(server[1]) : 3389),
                    new VmInstanceSettings()
                    {

                    });
            }
            catch (Exception e)
            {
                ExceptionDialog.Show(this, "RDP failed", e);
            }
        }
    }
}
