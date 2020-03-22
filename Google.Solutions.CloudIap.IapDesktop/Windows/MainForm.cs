using Google.Solutions.CloudIap.IapDesktop.Application.Settings;
using Google.Solutions.CloudIap.IapDesktop.ProjectExplorer;
using Google.Solutions.CloudIap.IapDesktop.RemoteDesktop;
using Google.Solutions.CloudIap.IapDesktop.Settings;
using Google.Solutions.Compute.Auth;
using Google.Solutions.Compute.Iap;
using Google.Solutions.IapDesktop.Application.Adapters;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.CloudIap.IapDesktop.Windows
{
    public interface IMainForm
    {
        void Close();
    }

    public partial class MainForm : Form, IJobHost, IMainForm, IAuthorizationService
    {
        private readonly WindowSettingsRepository windowSettings;
        private readonly AuthSettingsRepository authSettings;
        private readonly InventorySettingsRepository inventorySettings;

        private WaitDialog waitDialog = null;

        public MainForm()
        {
            this.windowSettings = Program.Services.GetService<WindowSettingsRepository>();
            this.authSettings = Program.Services.GetService<AuthSettingsRepository>();
            this.inventorySettings = Program.Services.GetService<InventorySettingsRepository>();

            Program.Services.AddSingleton<IMainForm>(this);
            Program.Services.AddSingleton<IAuthorizationService>(this);
            Program.Services.AddSingleton(new JobService(this, Program.Services));
            Program.Services.AddSingleton<IEventService>(new EventService(this));
            Program.Services.AddTransient<ProjectInventoryService>();
            Program.Services.AddTransient<ResourceManagerAdapter>();
            Program.Services.AddTransient<ComputeEngineAdapter>();

            // 
            // Restore window settings.
            //
            var windowSettings = this.windowSettings.GetSettings();
            if (windowSettings.IsMainWindowMaximized)
            {
                this.WindowState = FormWindowState.Maximized;
                InitializeComponent();
            }
            else if (windowSettings.MainWindowHeight != 0 &&
                     windowSettings.MainWindowWidth != 0)
            {
                InitializeComponent();
                this.Size = new Size(
                    windowSettings.MainWindowWidth,
                    windowSettings.MainWindowHeight);
            }
            else
            {
                InitializeComponent();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.windowSettings.SetSettings(new WindowSettings()
            {
                IsMainWindowMaximized = this.WindowState == FormWindowState.Maximized,
                MainWindowHeight = this.Size.Height,
                MainWindowWidth = this.Size.Width
            });
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            //
            // Authorize.
            //
            this.Authorization = AuthorizeDialog.Authorize(
                this,
                OAuthClient.Secrets,
                new[] { IapTunnelingEndpoint.RequiredScope },
                this.authSettings);

            if (this.Authorization == null)
            {
                // Not authorized -> close.
                Close();
            }

            // 
            // Set up sub-windows.
            //
            SuspendLayout();

            this.dockPanel.Theme = this.vs2015LightTheme;
            this.vsToolStripExtender.SetStyle(
                this.mainMenu,
                VisualStudioToolStripExtender.VsVersion.Vs2015,
                this.vs2015LightTheme);
            this.vsToolStripExtender.SetStyle(
                this.statusStrip,
                VisualStudioToolStripExtender.VsVersion.Vs2015,
                this.vs2015LightTheme);



            Program.Services.AddSingleton<RemoteDesktopService>(new RemoteDesktopService(this.dockPanel));
            Program.Services.AddSingleton<ISettingsEditor>(new SettingsEditorWindow(this.dockPanel));
            Program.Services.AddSingleton<IProjectExplorer>(new ProjectExplorerWindow(this.dockPanel));

            var settingsWindow = new SettingsEditorWindow();
            settingsWindow.Show(dockPanel, DockState.DockRight);
            //settingsWindow.Show(projectExplorer.Pane, DockAlignment.Bottom, 0.3);

#if DEBUG
            var debugWindow = new DebugWindow();
            debugWindow.Show(dockPanel, DockState.DockRight);
#endif



            ResumeLayout();
        }


        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutWindow().ShowDialog(this);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void projectExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.Services.GetService<IProjectExplorer>().ShowWindow();
        }

        //---------------------------------------------------------------------
        // IAuthorizationService.
        //---------------------------------------------------------------------

        public IAuthorization Authorization { get; private set; }

        //---------------------------------------------------------------------
        // IEventRoutingHost.
        //---------------------------------------------------------------------

        public ISynchronizeInvoke Invoker => this;

        public bool IsWaitDialogShowing
        {
            get
            {
                // Capture variable in local context first to avoid a race condition.
                var dialog = this.waitDialog;
                return dialog != null && dialog.IsShowing;
            }
        }

        public void ShowWaitDialog(JobDescription jobDescription, CancellationTokenSource cts)
        {
            Debug.Assert(!this.Invoker.InvokeRequired, "ShowWaitDialog must be called on UI thread");

            this.waitDialog = new WaitDialog(jobDescription.StatusMessage, cts);
            this.waitDialog.ShowDialog(this);
        }

        public void CloseWaitDialog()
        {
            Debug.Assert(!this.Invoker.InvokeRequired, "CloseWaitDialog must be called on UI thread");
            Debug.Assert(this.waitDialog != null);

            this.waitDialog.Close();
        }

        public bool ConfirmReauthorization()
        {
            return MessageBox.Show(
                this,
                "Your session has expired or the authorization has been revoked. " +
                "Do you want to sign in again?",
                "Authorization required",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning) == DialogResult.Yes;
        }
    }

    internal abstract class AsyncEvent
    {
        public string WaitMessage { get; }

        protected AsyncEvent(string message)
        {
            this.WaitMessage = message;
        }
    }
}
