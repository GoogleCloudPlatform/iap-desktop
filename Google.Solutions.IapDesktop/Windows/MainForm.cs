using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Windows.RemoteDesktop;
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
using Google.Solutions.CloudIap;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.SettingsEditor;
using Google.Solutions.IapDesktop.Application;

namespace Google.Solutions.IapDesktop.Windows
{
    
    public partial class MainForm : Form, IJobHost, IMainForm, IAuthorizationService
    {
        private readonly WindowSettingsRepository windowSettings;
        private readonly AuthSettingsRepository authSettings;
        private readonly InventorySettingsRepository inventorySettings;

        private WaitDialog waitDialog = null;

        public MainForm()
        {
            this.windowSettings = TempProgram.Services.GetService<WindowSettingsRepository>();
            this.authSettings = TempProgram.Services.GetService<AuthSettingsRepository>();
            this.inventorySettings = TempProgram.Services.GetService<InventorySettingsRepository>();

            TempProgram.Services.AddSingleton<IMainForm>(this);
            TempProgram.Services.AddSingleton<IAuthorizationService>(this);
            TempProgram.Services.AddSingleton(new JobService(this, TempProgram.Services));
            TempProgram.Services.AddSingleton<IEventService>(new EventService(this));
            TempProgram.Services.AddTransient<ProjectInventoryService>();
            TempProgram.Services.AddTransient<ResourceManagerAdapter>();
            TempProgram.Services.AddTransient<ComputeEngineAdapter>();
            TempProgram.Services.AddTransient<CloudConsoleService>();

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



            TempProgram.Services.AddSingleton<RemoteDesktopService>();
            TempProgram.Services.AddSingleton<ISettingsEditor, SettingsEditorWindow>();
            TempProgram.Services.AddSingleton<IProjectExplorer, ProjectExplorerWindow>();

            //settingsWindow.Show(projectExplorer.Pane, DockAlignment.Bottom, 0.3);

#if DEBUG
            TempProgram.Services.AddTransient<DebugWindow>();

            TempProgram.Services.GetService<DebugWindow>().Show(dockPanel, DockState.DockRight);
#endif



            ResumeLayout();
        }

        //---------------------------------------------------------------------
        // IMainForm.
        //---------------------------------------------------------------------

        public DockPanel MainPanel => this.dockPanel;

        //---------------------------------------------------------------------
        // Main menu events.
        //---------------------------------------------------------------------


        private void aboutToolStripMenuItem_Click(object sender, EventArgs _)
        {
            new AboutWindow().ShowDialog(this);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs _)
        {
            Close();
        }

        private async void signoutToolStripMenuItem_Click(object sender, EventArgs _)
        {
            try
            {
                await this.Authorization.RevokeAsync();
                MessageBox.Show(
                    this,
                    "The authorization for this application has been revoked.\n\n"+
                    "You will be prompted to sign in again the next time you start the application.",
                    "Signed out",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception e)
            {
                ExceptionDialog.Show(this, "Sign out", e);
            }
        }

        private void projectExplorerToolStripMenuItem_Click(object sender, EventArgs _)
        {
            TempProgram.Services.GetService<IProjectExplorer>().ShowWindow();
        }

        private void openIapDocsToolStripMenuItem_Click(object sender, EventArgs _)
        {
            TempProgram.Services.GetService<CloudConsoleService>().OpenIapOverviewDocs();
        }

        private void openIapAccessDocsToolStripMenuItem_Click(object sender, EventArgs _)
        {
            TempProgram.Services.GetService<CloudConsoleService>().OpenIapAccessDocs();
        }

        private async void addProjectToolStripMenuItem_Click(object sender, EventArgs _)
        {
            try
            {
                await TempProgram.Services.GetService<IProjectExplorer>().ShowAddProjectDialogAsync();
            }
            catch (TaskCanceledException)
            {
                // Ignore.
            }
            catch (Exception e)
            {
                ExceptionDialog.Show(this, "Adding project failed", e);
            }
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
