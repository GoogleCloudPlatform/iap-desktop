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

using Google.Apis.Util;
using Google.Solutions.Common.Auth;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.About;
using Google.Solutions.IapDesktop.Application.Views.Authentication;
using Google.Solutions.IapDesktop.Application.Views.Diagnostics;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Application.Views.Options;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CA1031 // Do not catch general exception types

namespace Google.Solutions.IapDesktop.Windows
{
    public partial class MainForm : Form, IJobHost, IMainForm, IAuthorizationAdapter
    {
        private readonly MainFormViewModel viewModel;

        private readonly ApplicationSettingsRepository applicationSettings;
        private readonly IServiceProvider serviceProvider;
        private IIapUrlHandler urlHandler;

        public IapRdpUrl StartupUrl { get; set; }
        public CommandContainer<IMainForm> ViewMenu { get; }

        public MainForm(IServiceProvider bootstrappingServiceProvider, IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            this.applicationSettings = bootstrappingServiceProvider.GetService<ApplicationSettingsRepository>();

            // 
            // Restore window settings.
            //
            var windowSettings = this.applicationSettings.GetSettings();
            if (windowSettings.IsMainWindowMaximized.BoolValue)
            {
                this.WindowState = FormWindowState.Maximized;
                InitializeComponent();
            }
            else if (windowSettings.MainWindowHeight.IntValue != 0 &&
                     windowSettings.MainWindowWidth.IntValue != 0)
            {
                InitializeComponent();
                this.Size = new Size(
                    windowSettings.MainWindowWidth.IntValue,
                    windowSettings.MainWindowHeight.IntValue);
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
            this.Text = GeneralOptionsViewModel.FriendlyName;
            this.ViewMenu = new CommandContainer<IMainForm>(
                this,
                this.viewToolStripMenuItem.DropDownItems,
                ToolStripItemDisplayStyle.ImageAndText,
                this.serviceProvider)
            {
                Context = this // There is no real context for this.
            };

            this.viewModel = new MainFormViewModel(
                this,
                this.vs2015LightTheme.ColorPalette,
                bootstrappingServiceProvider.GetService<ApplicationSettingsRepository>(),
                bootstrappingServiceProvider.GetService<AuthSettingsRepository>());

            this.reportInternalIssueToolStripMenuItem.BindProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsReportInternalIssueVisible,
                this.components);

            // Status bar.
            this.statusStrip.BindProperty(
                c => c.BackColor,
                this.viewModel,
                m => m.StatusBarBackColor,
                this.components);
            this.toolStripStatus.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.StatusText,
                this.components);
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
            this.toolStripSignInStateButton.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.SignInStateCaption,
                this.components);
            this.toolStripDeviceStateButton.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.DeviceStateCaption,
                this.components);
            this.toolStripDeviceStateButton.BindProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsDeviceStateVisible,
                this.components);

            // Logging.
            this.enableloggingToolStripMenuItem.BindProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsLoggingEnabled,
                this.components);
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            var settings = this.applicationSettings.GetSettings();

            if (settings.IsUpdateCheckEnabled.BoolValue &&
                (DateTime.UtcNow - DateTime.FromBinary(settings.LastUpdateCheck.LongValue)).Days > 7)
            {
                // Time to check for updates again.
                try
                {
                    var updateService = this.serviceProvider.GetService<IUpdateService>();
                    updateService.CheckForUpdates(
                        this,
                        TimeSpan.FromSeconds(5),
                        out bool donotCheckForUpdatesAgain);

                    settings.IsUpdateCheckEnabled.BoolValue = !donotCheckForUpdatesAgain;
                    settings.LastUpdateCheck.LongValue = DateTime.UtcNow.ToBinary();
                }
                catch (Exception)
                {
                    // Nevermind.
                }
            }

            // Save window state.
            settings.IsMainWindowMaximized.BoolValue = this.WindowState == FormWindowState.Maximized;
            settings.MainWindowHeight.IntValue = this.Size.Height;
            settings.MainWindowWidth.IntValue = this.Size.Width;

            this.applicationSettings.SetSettings(settings);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }

        private void MainForm_Shown(object sender, EventArgs args)
        {
            //
            // Authorize.
            //
            while (this.viewModel.Authorization == null)
            {
                try
                {
                    // NB. If the user cancels, no exception is thrown.
                    this.viewModel.Authorize();
                }
                catch (Exception e)
                {
                    //
                    // This exception might be due to a missing/incorrect proxy
                    // configuration, so give the user a chance to change proxy
                    // settings.
                    //

                    try
                    {
                        if (this.serviceProvider.GetService<ITaskDialog>()
                            .ShowOptionsTaskDialog(
                                this,
                                TaskDialogIcons.TD_ERROR_ICON,
                                "Authorization failed",
                                "IAP Desktop failed to complete the OAuth authorization. " +
                                    "This might be due to network communication issues.",
                                e.Message,
                                e.FullMessage(),
                                new[]
                                {
                                    "Change network settings"
                                },
                                null,
                                out bool _) == 0)
                        {
                            // Open settings.
                            if (this.serviceProvider.GetService<OptionsDialog>().ShowDialog(this) == DialogResult.OK)
                            {
                                // Ok, retry with modified settings.
                                continue;
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    { }
                }

                if (!this.viewModel.IsAuthorized)
                {
                    // Not authorized, either because the user cancelled or an 
                    // error occured -> close.
                    Close();
                    return;
                }
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
            var debugCommand = this.ViewMenu.AddCommand(
                new Command<IMainForm>(
                    "Debug",
                    _ => CommandState.Enabled,
                    context => { }));
            debugCommand.AddCommand(new Command<IMainForm>(
                "Job Service",
                _ => CommandState.Enabled,
                _ => this.serviceProvider.GetService<DebugJobServiceWindow>().ShowWindow()));
            debugCommand.AddCommand(new Command<IMainForm>(
                "Docking ",
                _ => CommandState.Enabled,
                _ => this.serviceProvider.GetService<DebugDockingWindow>().ShowWindow()));
            debugCommand.AddCommand(new Command<IMainForm>(
                "Project Explorer Tracking",
                _ => CommandState.Enabled,
                _ => this.serviceProvider.GetService<DebugProjectExplorerTrackingWindow>().ShowWindow()));
            debugCommand.AddCommand(new Command<IMainForm>(
                "Full screen pane",
                _ => CommandState.Enabled,
                _ => this.serviceProvider.GetService<DebugFullScreenPane>().ShowWindow()));
            debugCommand.AddCommand(new Command<IMainForm>(
                "Track window focus",
                _ => CommandState.Enabled,
                _ => this.serviceProvider.GetService<DebugFocusWindow>().ShowWindow()));
#endif
        }

        internal void ConnectToUrl(IapRdpUrl url)
        {
            if (this.urlHandler != null)
            {
                try
                {
                    this.urlHandler
                        .ActivateOrConnectInstanceAsync(url)
                        .ContinueWith(t => this.serviceProvider
                                .GetService<IExceptionDialog>()
                                .Show(this, "Failed to connect to VM instance", t.Exception),
                            CancellationToken.None,
                            TaskContinuationOptions.OnlyOnFaulted,
                            TaskScheduler.FromCurrentSynchronizationContext());
                }
                catch (Exception e) when (e.IsCancellation())
                {
                    // The user cancelled, nervemind.
                }
            }
        }

        //---------------------------------------------------------------------
        // IMainForm.
        //---------------------------------------------------------------------

        public IWin32Window Window => this;
        public DockPanel MainPanel => this.dockPanel;

        public void SetUrlHandler(IIapUrlHandler handler)
        {
            Utilities.ThrowIfNull(handler, nameof(handler));
            this.urlHandler = handler;
        }

        public CommandContainer<IMainForm> AddMenu(string caption, int? index)
        {
            var menu = new ToolStripMenuItem(caption);

            if (index.HasValue)
            {
                this.mainMenu.Items.Insert(
                    Math.Min(index.Value, this.mainMenu.Items.Count),
                    menu);
            }
            else
            {
                this.mainMenu.Items.Add(menu);
            }

            var commandContainer = new CommandContainer<IMainForm>(
                this,
                menu.DropDownItems,
                ToolStripItemDisplayStyle.ImageAndText,
                this.serviceProvider)
            {
                Context = this // There is no real context for this.
            };

            menu.DropDownOpening += (sender, args) =>
            {
                // Force re-evaluation of context.
                commandContainer.Context = this;
            };

            return commandContainer;
        }
        public void Minimize()
        {
            this.WindowState = FormWindowState.Minimized;
        }

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
                Close();
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
            this.serviceProvider.GetService<HelpService>().OpenTopic(HelpTopics.IapOverview);
        }

        private void openSecureConnectDocsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.serviceProvider.GetService<HelpService>().OpenTopic(HelpTopics.SecureConnectDcaOverview);
        }

        private void openIapAccessDocsToolStripMenuItem_Click(object sender, EventArgs _)
        {
            this.serviceProvider.GetService<HelpService>().OpenTopic(HelpTopics.IapAccess);
        }

        private void openIapFirewallDocsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.serviceProvider.GetService<HelpService>().OpenTopic(HelpTopics.CreateIapFirewallRule);
        }

        private void reportGithubIssueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.serviceProvider.GetService<GithubAdapter>().ReportIssue();
        }

        private void reportInternalIssueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.serviceProvider.GetService<BuganizerAdapter>().ReportIssue();
        }

        private void viewHelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.serviceProvider.GetService<HelpService>().OpenTopic(HelpTopics.General);
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
            try
            {
                // Toggle logging state.
                this.viewModel.IsLoggingEnabled = !this.viewModel.IsLoggingEnabled;
            }
            catch (Exception e)
            {
                this.serviceProvider
                    .GetService<IExceptionDialog>()
                    .Show(this, "Configuring logging failed", e);
            }
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs _)
        {
            try
            {
                new OptionsDialog((IServiceCategoryProvider)this.serviceProvider).ShowDialog(this);
            }
            catch (TaskCanceledException)
            {
                // Ignore.
            }
            catch (Exception e)
            {
                this.serviceProvider
                    .GetService<IExceptionDialog>()
                    .Show(this, "Opening Options window failed", e);
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

#pragma warning disable IDE0067 // Dispose objects before losing scope

        private void toolStripEmailButton_Click(object sender, EventArgs e)
        {
            var button = (ToolStripItem)sender;
            var screenPosition = new Rectangle(
                this.statusStrip.PointToScreen(button.Bounds.Location),
                button.Size);

            new UserFlyoutWindow(
                    new UserFlyoutViewModel(
                        this.Authorization,
                        this.serviceProvider.GetService<ICloudConsoleService>()))
                .Show(
                    this,
                    screenPosition,
                    ContentAlignment.TopLeft);
        }

        private void toolStripDeviceStateButton_Click(object sender, EventArgs e)
        {
            var button = (ToolStripItem)sender;
            var screenPosition = new Rectangle(
                this.statusStrip.PointToScreen(button.Bounds.Location),
                button.Size);

            new DeviceFlyoutWindow(
                    new DeviceFlyoutViewModel(this, this.DeviceEnrollment))
                .Show(
                    this,
                    screenPosition,
                    ContentAlignment.TopLeft);
        }

#pragma warning restore IDE0067 // Dispose objects before losing scope

        //---------------------------------------------------------------------
        // IAuthorizationAdapter.
        //---------------------------------------------------------------------

        public IAuthorization Authorization => this.viewModel.Authorization;
        public IDeviceEnrollment DeviceEnrollment => this.viewModel.DeviceEnrollment;

        public Task ReauthorizeAsync(CancellationToken token)
            => this.viewModel.ReauthorizeAsync(token);

    }
}
