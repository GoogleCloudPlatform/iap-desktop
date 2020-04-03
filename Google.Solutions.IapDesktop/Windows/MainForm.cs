//
// Copyright 2020 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using Google.Solutions.CloudIap;
using Google.Solutions.Compute.Auth;
using Google.Solutions.Compute.Iap;
using Google.Solutions.IapDesktop.Application.Adapters;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Services;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.TunnelsViewer;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Windows
{

    public partial class MainForm : Form, IJobHost, IMainForm, IAuthorizationService
    {
        private readonly ApplicationSettingsRepository applicationSettings;
        private readonly AuthSettingsRepository authSettings;
        private readonly InventorySettingsRepository inventorySettings;
        private readonly IServiceProvider serviceProvider;

        private WaitDialog waitDialog = null;

        public MainForm(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.applicationSettings = serviceProvider.GetService<ApplicationSettingsRepository>();
            this.authSettings = serviceProvider.GetService<AuthSettingsRepository>();
            this.inventorySettings = serviceProvider.GetService<InventorySettingsRepository>();

            // 
            // Restore window settings.
            //
            var windowSettings = this.applicationSettings.GetSettings();
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

            // Set fixed size for the left/right panels.
            this.dockPanel.DockLeftPortion =
                this.dockPanel.DockRightPortion = (300.0f / this.Width);

            this.checkForUpdatesOnExitToolStripMenuItem.Checked 
                = this.applicationSettings.GetSettings().IsUpdateCheckEnabled;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            var settings = this.applicationSettings.GetSettings();

            if (settings.IsUpdateCheckEnabled &&
                (DateTime.UtcNow - DateTime.FromBinary(settings.LastUpdateCheck)).Days > 7)
            {
                // Time to check for updates again.
                try
                {
                    var updateService = this.serviceProvider.GetService<IUpdateService>();
                    updateService.CheckForUpdates(
                        this,
                        TimeSpan.FromSeconds(5),
                        out bool donotCheckForUpdatesAgain);

                    settings.IsUpdateCheckEnabled = !donotCheckForUpdatesAgain;
                    settings.LastUpdateCheck = DateTime.UtcNow.ToBinary();
                }
                catch (Exception)
                {
                    // Nevermind.
                }
            }

            // Save window state.
            settings.IsMainWindowMaximized = this.WindowState == FormWindowState.Maximized;
            settings.MainWindowHeight = this.Size.Height;
            settings.MainWindowWidth = this.Size.Width;

            this.applicationSettings.SetSettings(settings);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }

        private void MainForm_Shown(object sender, EventArgs _)
        {
            //
            // Authorize.
            //
            try
            {
                this.Authorization = AuthorizeDialog.Authorize(
                    this,
                    OAuthClient.Secrets,
                    new[] { IapTunnelingEndpoint.RequiredScope },
                    this.authSettings);
            }
            catch (Exception e)
            {
                this.serviceProvider
                    .GetService<IExceptionDialog>()
                    .Show(this, "Authorization failed", e);
            }

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

            ResumeLayout();

            this.serviceProvider.GetService<IProjectExplorer>().ShowWindow();

#if DEBUG
            this.serviceProvider.GetService<DebugWindow>().ShowWindow();
#endif
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
            this.serviceProvider.GetService<AboutWindow>().ShowDialog(this);
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
                    "The authorization for this application has been revoked.\n\n" +
                    "You will be prompted to sign in again the next time you start the application.",
                    "Signed out",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception e)
            {
                this.serviceProvider
                    .GetService<IExceptionDialog>()
                    .Show(this, "Sign out", e);
            }
        }

        private void projectExplorerToolStripMenuItem_Click(object sender, EventArgs _)
        {
            this.serviceProvider.GetService<IProjectExplorer>().ShowWindow();
        }

        private void openIapDocsToolStripMenuItem_Click(object sender, EventArgs _)
        {
            this.serviceProvider.GetService<CloudConsoleService>().OpenIapOverviewDocs();
        }

        private void openIapAccessDocsToolStripMenuItem_Click(object sender, EventArgs _)
        {
            this.serviceProvider.GetService<CloudConsoleService>().OpenIapAccessDocs();
        }

        private void activeTunnelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.serviceProvider.GetService<ITunnelsViewer>().ShowWindow();
        }

        private void reportIssueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.serviceProvider.GetService<GithubAdapter>().ReportIssue();
        }

        private async void addProjectToolStripMenuItem_Click(object sender, EventArgs _)
        {
            try
            {
                await this.serviceProvider.GetService<IProjectExplorer>().ShowAddProjectDialogAsync();
            }
            catch (TaskCanceledException)
            {
                // Ignore.
            }
            catch (Exception e)
            {
                this.serviceProvider
                    .GetService<IExceptionDialog>()
                    .Show(this, "Adding project failed", e);
            }
        }

        private void enableloggingToolStripMenuItem_Click(object sender, EventArgs _)
        {
            var loggingEnabled = 
                this.enableloggingToolStripMenuItem.Checked = 
                !this.enableloggingToolStripMenuItem.Checked;

            try
            {
                Program.IsLoggingEnabled = loggingEnabled;

                if (loggingEnabled)
                {
                    this.toolStripStatusLabel.Text = $"Logging to {Program.LogFile}, performance " +
                        "might be degraded while logging is enabled.";
                    this.statusStrip.BackColor = Color.Red;
                }
                else
                {
                    this.toolStripStatusLabel.Text = string.Empty;
                    this.statusStrip.BackColor = this.vs2015LightTheme.ColorPalette.ToolWindowCaptionActive.Background;
                }
            }
            catch (Exception e)
            {
                this.serviceProvider
                    .GetService<IExceptionDialog>()
                    .Show(this, "Configuring logging failed", e);
            }
        }

        private void checkForUpdatesOnExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var updateEnabled = !checkForUpdatesOnExitToolStripMenuItem.Checked;

            var settings = this.applicationSettings.GetSettings();
            settings.IsUpdateCheckEnabled = updateEnabled;
            this.applicationSettings.SetSettings(settings);

            // Toggle menu.
            checkForUpdatesOnExitToolStripMenuItem.Checked =
                !checkForUpdatesOnExitToolStripMenuItem.Checked;
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
