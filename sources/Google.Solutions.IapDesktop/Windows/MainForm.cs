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

using Google.Solutions.Common.Auth;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Services.Windows;
using Google.Solutions.IapDesktop.Application.Services.Windows.Diagnostics;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Services.Windows.RemoteDesktop;
using Google.Solutions.IapDesktop.Application.Services.Windows.TunnelsViewer;
using Google.Solutions.IapDesktop.Application.Services.Workflows;
using Google.Solutions.IapDesktop.Application.Util;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

#pragma warning disable IDE1006 // Naming Styles

namespace Google.Solutions.IapDesktop.Windows
{
    public partial class MainForm : Form, IJobHost, IMainForm, IAuthorizationAdapter
    {
        private readonly MainFormViewModel viewModel;

        private readonly ApplicationSettingsRepository applicationSettings;
        private readonly IServiceProvider serviceProvider;

        public IapRdpUrl StartupUrl { get; set; }
        public CommandContainer<IMainForm> ViewCommands { get; }

        public MainForm(IServiceProvider bootstrappingServiceProvider, IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            this.applicationSettings = bootstrappingServiceProvider.GetService<ApplicationSettingsRepository>();

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

            // Set fixed size for the left/right panels (in pixels).
            this.dockPanel.DockLeftPortion =
                this.dockPanel.DockRightPortion = 300.0f;

            //
            // Bind controls.
            //
            this.Text = MainFormViewModel.FriendlyName;
            this.ViewCommands = new CommandContainer<IMainForm>(
                this,
                this.viewToolStripMenuItem.DropDownItems,
                ToolStripItemDisplayStyle.ImageAndText,
                this.serviceProvider);
            this.ViewCommands.Context = this; // There is no real context for this.

            this.viewModel = new MainFormViewModel(
                this,
                this.applicationSettings,
                bootstrappingServiceProvider.GetService<AuthSettingsRepository>(),
                bootstrappingServiceProvider.GetService<AppProtocolRegistry>());

            // Status bar.
            this.backgroundJobLabel.BindProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsBackgroundJobStatusVisible,
                this.components);
            this.cancelBackgroundJobsButton.BindProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsBackgroundJobStatusVisible,
                this.components);
            this.backgroundJobLabel.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.BackgroundJobStatus,
                this.components);
            this.toolStripEmail.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.UserEmail,
                this.components);

            // Menus.
            this.checkForUpdatesOnExitToolStripMenuItem.BindProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsUpdateCheckEnabled,
                this.components);
            this.enableAppProtocolToolStripMenuItem.BindProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsProtocolRegistred,
                this.components);
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
                this.viewModel.Authorize();
            }
            catch (Exception e)
            {
                this.serviceProvider
                    .GetService<IExceptionDialog>()
                    .Show(this, "Authorization failed", e);
            }

            if (this.viewModel.Authorization == null)
            {
                // Not authorized -> close.
                Close();
                return;
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

            if (this.StartupUrl != null)
            {
                // Dispatch URL.
                ConnectToUrl(this.StartupUrl);
            }
            else
            {
                // No startup URL provided, just show project explorer then.
                this.serviceProvider.GetService<IProjectExplorer>().ShowWindow();
            }

#if DEBUG
            this.serviceProvider.GetService<DebugJobServiceWindow>().ShowWindow();
            this.serviceProvider.GetService<DebugProjectExplorerTrackingWindow>().ShowWindow();
#endif
        }

        internal void ConnectToUrl(IapRdpUrl url)
        {
            try
            {
                this.serviceProvider
                    .GetService<IIapUrlHandler>()
                    .ActivateOrConnectInstanceAsync(url)
                    .ContinueWith(t => this.serviceProvider
                            .GetService<IExceptionDialog>()
                            .Show(this, "Failed to connect to VM instance", t.Exception),
                        CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted,
                        TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (UnknownServiceException e)
            {
                this.serviceProvider
                    .GetService<IExceptionDialog>()
                    .Show(this, "Handler not found for URL", e);
            }
            catch (Exception e) when (e.IsCancellation())
            {
                // The user cancelled, nervemind.
            }
        }

        //---------------------------------------------------------------------
        // IMainForm.
        //---------------------------------------------------------------------

        public IWin32Window Window => this;
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
                await this.viewModel.RevokeAuthorizationAsync().ConfigureAwait(true);
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
                await this.serviceProvider.GetService<IProjectExplorer>()
                    .ShowAddProjectDialogAsync()
                    .ConfigureAwait(true);
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
                    this.toolStripStatus.Text = $"Logging to {Program.LogFile}, performance " +
                        "might be degraded while logging is enabled.";
                    this.statusStrip.BackColor = Color.Red;
                }
                else
                {
                    this.toolStripStatus.Text = string.Empty;
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

        private void enableAppProtocolToolStripMenuItem_Click(object sender, EventArgs e) 
        {
            this.enableAppProtocolToolStripMenuItem.Checked =
                !this.enableAppProtocolToolStripMenuItem.Checked;
        }

        private void checkForUpdatesOnExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            checkForUpdatesOnExitToolStripMenuItem.Checked =
                !checkForUpdatesOnExitToolStripMenuItem.Checked;
        }

        private void desktopToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            var session = this.serviceProvider.GetService<IRemoteDesktopService>().ActiveSession;
            foreach (var item in this.desktopToolStripMenuItem.DropDownItems.OfType<ToolStripDropDownItem>())
            {
                item.Enabled = session != null && session.IsConnected;
            }
        }

        private void fullScreenToolStripMenuItem_Click(object sender, EventArgs args)
            => DoWithActiveSession(session => session.TrySetFullscreen(true));

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs args)
            => DoWithActiveSession(session => session.Close());

        private void showSecurityScreenToolStripMenuItem_Click(object sender, EventArgs args)
            => DoWithActiveSession(session => session.ShowSecurityScreen());

        private void showtaskManagerToolStripMenuItem_Click(object sender, EventArgs args)
            => DoWithActiveSession(session => session.ShowTaskManager());

        private void DoWithActiveSession(Action<IRemoteDesktopSession> action)
        {
            try
            {
                var session = this.serviceProvider.GetService<IRemoteDesktopService>().ActiveSession;
                if (session != null)
                {
                    action(session);
                }
            }
            catch (Exception e)
            {
                this.serviceProvider
                    .GetService<IExceptionDialog>()
                    .Show(this, "Remote Desktop action failed", e);
            }
        }

        //---------------------------------------------------------------------
        // IJobHost.
        //---------------------------------------------------------------------

        public ISynchronizeInvoke Invoker => this;

        public IJobUserFeedback ShowFeedback(
            JobDescription jobDescription,
            CancellationTokenSource cancellationSource)
        {
            Debug.Assert(!this.Invoker.InvokeRequired, "ShowForegroundFeedback must be called on UI thread");

            switch (jobDescription.Feedback)
            {
                case JobUserFeedbackType.ForegroundFeedback:
                    // Show WaitDialog, blocking all user intraction.
                    return new WaitDialog(this, jobDescription.StatusMessage, cancellationSource);

                default:
                    return this.viewModel.CreateBackgroundJob(jobDescription, cancellationSource);
            }
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

        private void cancelBackgroundJobsButton_Click(object sender, EventArgs e)
            => this.viewModel.CancelBackgroundJobs();

        //---------------------------------------------------------------------
        // IAuthorizationAdapter.
        //---------------------------------------------------------------------

        public IAuthorization Authorization => this.viewModel.Authorization;

        public Task ReauthorizeAsync(CancellationToken token)
            => this.viewModel.ReauthorizeAsync(token);
    }
}
