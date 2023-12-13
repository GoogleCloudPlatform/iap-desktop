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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Common.Interop;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.ToolWindows.Update;
using Google.Solutions.IapDesktop.Application.ToolWindows.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.About;
using Google.Solutions.IapDesktop.Application.Windows.Auth;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Application.Windows.Options;
using Google.Solutions.IapDesktop.Application.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Mvvm.Drawing;
using Google.Solutions.Mvvm.Shell;
using Google.Solutions.Platform.Net;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using Google.Solutions.Apis.Auth.Gaia;
using static System.Collections.Specialized.BitVector32;
using Google.Solutions.IapDesktop.Application.Windows.Update;

#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CA1031 // Do not catch general exception types

namespace Google.Solutions.IapDesktop.Windows
{
    public partial class MainForm : Form, IJobHost, IMainWindow
    {
        //
        // Calculate minimum size so that it's a quarter of a 1080p screen,
        // leaving some room for the taskbar (typically <= 50px).
        //
        private static readonly Size MinimumWindowSize = new Size(
            1920 / 2,
            (1080 - 50) / 2);

        private readonly MainFormViewModel viewModel;

        private readonly IThemeService themeService;
        private readonly IRepository<IApplicationSettings> applicationSettings;
        private readonly IServiceProvider serviceProvider;
        private readonly IBindingContext bindingContext;

        private readonly ContextSource<IMainWindow> viewMenuContextSource;
        private readonly ContextSource<ToolWindowViewBase> windowMenuContextSource;

        private readonly CommandContainer<IMainWindow> viewMenuCommands;
        private readonly CommandContainer<ToolWindowViewBase> windowMenuCommands;

        public bool ShowWhatsNew { get; set; } = false;
        public IapRdpUrl StartupUrl { get; set; }
        public ICommandContainer<IMainWindow> ViewMenu => this.viewMenuCommands;
        public ICommandContainer<ToolWindowViewBase> WindowMenu => this.windowMenuCommands;

        public MainForm(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            this.themeService = this.serviceProvider.GetService<IThemeService>();
            this.applicationSettings = this.serviceProvider.GetService<IRepository<IApplicationSettings>>();
            this.bindingContext = serviceProvider.GetService<IBindingContext>();

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

            SuspendLayout();

            this.themeService.MainWindowTheme.ApplyTo(this);

            this.MinimumSize = MinimumWindowSize;

            // Set fixed size for the left/right panels (in pixels).
            this.dockPanel.DockLeftPortion =
                this.dockPanel.DockRightPortion = 300.0f;

            //
            // View menu.
            //
            this.viewMenuContextSource = new ContextSource<IMainWindow>()
            {
                Context = this // Pseudo-context, never changes
            };

            this.viewMenuCommands = new CommandContainer<IMainWindow>(
                ToolStripItemDisplayStyle.ImageAndText,
                this.viewMenuContextSource,
                this.bindingContext);
            this.viewMenuCommands.BindTo(
                this.viewToolStripMenuItem,
                this.bindingContext);

            //
            // Window menu.
            //
            this.windowMenuContextSource = new ContextSource<ToolWindowViewBase>();

            this.windowToolStripMenuItem.DropDownOpening += (sender, args) =>
            {
                this.windowMenuContextSource.Context = this.dockPanel.ActiveContent as ToolWindowViewBase;
            };

            this.dockPanel.ActiveContentChanged += (sender, args) =>
            {
                //
                // NB. It's possible that ActiveContent is null although there
                // is an active document. Most commonly, this happens when
                // focus is released from an RDP window by using a keyboard
                // shortcut.
                //
                this.windowMenuContextSource.Context =
                    (this.dockPanel.ActiveContent ?? this.dockPanel.ActiveDocumentPane?.ActiveContent)
                        as ToolWindowViewBase;
            };

            this.windowMenuCommands = new CommandContainer<ToolWindowViewBase>(
                ToolStripItemDisplayStyle.ImageAndText,
                this.windowMenuContextSource,
                this.bindingContext);
            this.windowMenuCommands.BindTo(
                this.windowToolStripMenuItem,
                this.bindingContext);

            //
            // Bind controls.
            //
            this.viewModel = new MainFormViewModel(
                this,
                this.serviceProvider.GetService<IInstall>(),
                this.serviceProvider.GetService<UserProfile>(),
                this.serviceProvider.GetService<IAuthorization>());

            this.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.WindowTitle,
                this.bindingContext);

            //
            // Status bar.
            //
            this.statusStrip.BindReadonlyProperty(
                c => c.Active,
                this.viewModel,
                m => m.IsLoggingEnabled,
                this.bindingContext);
            this.toolStripStatus.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.StatusText,
                this.bindingContext);
            this.backgroundJobLabel.BindProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsBackgroundJobStatusVisible,
                this.bindingContext);
            this.cancelBackgroundJobsButton.BindProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsBackgroundJobStatusVisible,
                this.bindingContext);
            this.backgroundJobLabel.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.BackgroundJobStatus,
                this.bindingContext);
            this.profileStateButton.BindReadonlyProperty(
                c => c.Text,
                this.viewModel,
                m => m.ProfileStateCaption,
                this.bindingContext);

            //
            // Profile chooser.
            //
            var dynamicProfileMenuItemTag = new object();
            this.profileStateButton.DropDownOpening += (sender, args) =>
            {
                //
                // Re-populate list of profile menu items.
                //
                // Mark dynamic menu items with a tag so that we don't
                // accidentally remove any static menu items.
                //
                this.profileStateButton
                    .DropDownItems
                    .RemoveAll(item => item.Tag == dynamicProfileMenuItemTag)
                    .AddRange(this.viewModel
                        .AlternativeProfileNames
                        .Select(name => new ToolStripMenuItem(name)
                        {
                            Name = name,
                            Tag = dynamicProfileMenuItemTag
                        })
                        .ToArray());
            };

            this.profileStateButton.DropDownItemClicked += (sender, args) =>
            {
                if (args.ClickedItem.Tag == dynamicProfileMenuItemTag)
                {
                    this.viewModel.LaunchInstanceWithProfile(args.ClickedItem.Name);
                }
            };

            //
            // Logging.
            //
            this.enableloggingToolStripMenuItem.BindProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsLoggingEnabled,
                this.bindingContext);

            //
            // Bind menu commands.
            //
            this.WindowMenu.AddCommand(
                new ContextCommand<ToolWindowViewBase>(
                    "&Close",
                    window => window != null && window.IsDockable
                        ? CommandState.Enabled
                        : CommandState.Disabled,
                    window => window.CloseSafely()));
            this.WindowMenu.AddCommand(
                new ContextCommand<ToolWindowViewBase>(
                    "&Float",
                    window => window != null &&
                              !window.IsFloat &&
                              window.IsDockStateValid(DockState.Float)
                        ? CommandState.Enabled
                        : CommandState.Disabled,
                    window => window.IsFloat = true));
            this.WindowMenu.AddCommand(
                new ContextCommand<ToolWindowViewBase>(
                    "&Auto hide",
                    window => window != null && window.IsDocked && !window.IsAutoHide
                        ? CommandState.Enabled
                        : CommandState.Disabled,
                    window =>
                    {
                        window.IsAutoHide = true;
                        OnDockLayoutChanged();
                    })
                {
                    ShortcutKeys = Keys.Control | Keys.Alt | Keys.H
                });

            var dockCommand = this.WindowMenu.AddCommand(
                new ContextCommand<ToolWindowViewBase>(
                    "Dock",
                    _ => CommandState.Enabled,
                    context => { }));
            dockCommand.AddCommand(CreateDockCommand(
                "&Left",
                DockState.DockLeft,
                Keys.Control | Keys.Alt | Keys.Left));
            dockCommand.AddCommand(CreateDockCommand(
                "&Right",
                DockState.DockRight,
                Keys.Control | Keys.Alt | Keys.Right));
            dockCommand.AddCommand(CreateDockCommand(
                "&Top",
                DockState.DockTop,
                Keys.Control | Keys.Alt | Keys.Up));
            dockCommand.AddCommand(CreateDockCommand(
                "&Bottom",
                DockState.DockBottom,
                Keys.Control | Keys.Alt | Keys.Down));

            this.WindowMenu.AddSeparator();

            CommandState showTabCommand(ToolWindowViewBase window)
                => window != null && window.DockState == DockState.Document && window.Pane.Contents.Count > 1
                    ? CommandState.Enabled
                    : CommandState.Disabled;

            this.WindowMenu.AddCommand(
                new ContextCommand<ToolWindowViewBase>(
                    "&Next tab",
                    showTabCommand,
                    window => SwitchTab(window, 1))
                {
                    ShortcutKeys = Keys.Control | Keys.Alt | Keys.PageDown
                });
            this.WindowMenu.AddCommand(
                new ContextCommand<ToolWindowViewBase>(
                    "&Previous tab",
                    showTabCommand,
                    window => SwitchTab(window, -1))
                {
                    ShortcutKeys = Keys.Control | Keys.Alt | Keys.PageUp
                });
            this.WindowMenu.AddCommand(
                new ContextCommand<ToolWindowViewBase>(
                    "Capture/release &focus",
                    _ => this.dockPanel.ActiveDocumentPane != null &&
                         this.dockPanel.ActiveDocumentPane.Contents.EnsureNotNull().Any()
                        ? CommandState.Enabled
                        : CommandState.Disabled,
                    window => (this.dockPanel.ActiveDocumentPane?.ActiveContent as DocumentWindow)?.SwitchToDocument())
                {
                    ShortcutKeys = Keys.Control | Keys.Alt | Keys.Home
                });

            ResumeLayout();
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void MainForm_FormClosing(object sender, FormClosingEventArgs _)
        {
            var settings = this.applicationSettings.GetSettings();

            //
            // Check for updates.
            //
            var checkForUpdates = new CheckForUpdateCommand<IMainWindow>(
                this,
                this.serviceProvider.GetService<IInstall>(),
                this.serviceProvider.GetService<IUpdatePolicyFactory>(),
                this.serviceProvider.GetService<IReleaseFeed>(),
                this.serviceProvider.GetService<ITaskDialog>(),
                this.serviceProvider.GetService<IBrowser>());
            if (checkForUpdates.IsAutomatedCheckDue(
                DateTime.FromBinary(settings.LastUpdateCheck.LongValue)))
            {
                try
                {
                    using (var cts = new CancellationTokenSource())
                    {
                        //
                        // Check for updates. This check must be performed synchronously,
                        // otherwise this method returns and the application exits.
                        // In order not to block everything for too long in case of a network
                        // problem, use a timeout.
                        //
                        cts.CancelAfter(TimeSpan.FromSeconds(5));

                        checkForUpdates.Execute(this, cts.Token);

                        settings.LastUpdateCheck.LongValue = DateTime.UtcNow.ToBinary();
                    }
                }
                catch (Exception e)
                {
                    // Ignore in Release builds.
                    Debug.Fail(e.FullMessage());
                }
            }

            //
            // Save window state.
            //
            settings.IsMainWindowMaximized.BoolValue = this.WindowState == FormWindowState.Maximized;
            settings.MainWindowHeight.IntValue = this.Size.Height;
            settings.MainWindowWidth.IntValue = this.Size.Width;

            this.applicationSettings.SetSettings(settings);
        }

        private void MainForm_Shown(object sender, EventArgs __)
        {
            var profile = this.serviceProvider.GetService<UserProfile>();
            if (!profile.IsDefault)
            {
                //
                // Add taskbar badge to help distinguish this profile
                // from other profiles.
                //
                // NB. This can only be done after the window has been shown,
                // so this code must not be moved to the constructor.
                //
                using (var badge = BadgeIcon.ForTextInitial(profile.Name))
                using (var taskbar = ComReference.For((ITaskbarList3)new TaskbarList()))
                {
                    taskbar.Object.HrInit();
                    taskbar.Object.SetOverlayIcon(
                        this.Handle,
                        badge.Handle,
                        string.Empty);
                }
            }

            if (this.StartupUrl != null)
            {
                //
                // Dispatch URL.
                //
                ConnectToUrl(this.StartupUrl);
            }
            else
            {
                //
                // No URL provided, just show project explorer then.
                //
                this.serviceProvider
                    .GetService<IToolWindowHost>()
                    .GetToolWindow<ProjectExplorerView, ProjectExplorerViewModel>()
                    .Show();
            }

            if (this.ShowWhatsNew)
            {
                //
                // Show the "What's new" window (in addition to the project explorer).
                //
                var window = this.serviceProvider
                    .GetService<IToolWindowHost>()
                    .GetToolWindow<ReleaseNotesView, ReleaseNotesViewModel>();
                window.ViewModel.ShowAllReleases = false;
                window.Show();
            }
        }

        private void SwitchTab(ToolWindowViewBase reference, int delta)
        {
            //
            // Find a sibling tab and activate it. Make sure
            // to not run out of bounds.
            //
            var pane = this.dockPanel.ActiveDocumentPane;
            var windowIndex = pane.Contents.IndexOf(reference);
            var tabCount = pane.Contents.Count;
            if (pane.Contents[(tabCount + windowIndex + delta) % tabCount] is DocumentWindow sibling)
            {
                sibling.SwitchToDocument();
            }
        }

        private ContextCommand<ToolWindowViewBase> CreateDockCommand(
            string caption,
            DockState dockState,
            Keys shortcutKeys)
        {
            return new ContextCommand<ToolWindowViewBase>(
                caption,
                window => window != null &&
                            window.VisibleState != dockState &&
                            window.IsDockStateValid(dockState)
                    ? CommandState.Enabled
                    : CommandState.Disabled,
                window =>
                {
                    window.DockState = dockState;
                    OnDockLayoutChanged();
                })
            {
                ShortcutKeys = shortcutKeys
            };
        }

        private void OnDockLayoutChanged()
        {
            //
            // The DockPanel has a quirk where re-docking a
            // window doesn't cause the document to re-paint,
            // even if it changed positions.
            // To fix this, force the active document pane
            // to relayout.
            //
            this.dockPanel.ActiveDocumentPane?.PerformLayout();

            //
            // Force context refresh. This is necessary
            // of the command is triggered by a shortcut,
            // bypassing the menu-open event (which normally
            // updates the context).
            //
            this.windowMenuCommands.ForceRefresh();
        }

        private async Task ConnectToUrlAsync(IapRdpUrl url)
        {
            var command = this.serviceProvider.GetService<UrlCommands>().LaunchRdpUrl;
            if (command.QueryState(url) == CommandState.Enabled)
            {
                try
                {
                    await command
                        .ExecuteAsync(url)
                        .ConfigureAwait(true);
                }
                catch (Exception e) when (e.IsCancellation())
                {
                    // The user cancelled, nervemind.
                }
                catch (Exception e)
                {
                    this.serviceProvider
                        .GetService<IExceptionDialog>()
                        .Show(
                            this,
                            $"Connecting to the VM instance {url.Instance.Name} failed", e);
                }
            }
        }

        internal void ConnectToUrl(IapRdpUrl url)
        {
            ConnectToUrlAsync(url).ContinueWith(_ => { });
        }

        private void dockPanel_ActiveContentChanged(object sender, EventArgs e)
        {
            if (this.dockPanel.ActiveContent is ToolWindowViewBase toolWindow &&
                toolWindow.DockState == DockState.Document)
            {
                //
                // Focus switched to a document (we're not interested in
                // any other windows).
                //
                this.viewModel.SwitchToDocument(toolWindow.Text);
            }
            else if (
                this.dockPanel.ActiveContent == null ||
                !this.dockPanel.Documents.EnsureNotNull().Any())
            {
                //
                // All documents closed.
                //
                this.viewModel.SwitchToDocument(null);
            }
        }

        //---------------------------------------------------------------------
        // IMainForm.
        //---------------------------------------------------------------------

        public IWin32Window Window => this;
        public DockPanel MainPanel => this.dockPanel;

        public ICommandContainer<TContext> AddMenu<TContext>(
            string caption,
            int? index,
            Func<TContext> queryCurrentContextFunc)
            where TContext : class
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

            var container = new CommandContainer<TContext>(
                ToolStripItemDisplayStyle.ImageAndText,
                new CallbackSource<TContext>(queryCurrentContextFunc),
                this.bindingContext);
            container.BindTo(menu, this.bindingContext);

            menu.DropDownOpening += (sender, args) =>
            {
                //
                // Force refresh since we can't know if the context
                // has changed or not.
                //
                container.ForceRefresh();
            };

            return container;
        }

        public void Minimize()
        {
            this.WindowState = FormWindowState.Minimized;
        }

        public bool IsWindowThread()
        {
            return !this.InvokeRequired;
        }

        //---------------------------------------------------------------------
        // Main menu events.
        //---------------------------------------------------------------------

        private void aboutToolStripMenuItem_Click(object sender, EventArgs _)
        {
            using (var view = this.serviceProvider.GetDialog<AboutView, AboutViewModel>())
            {
                view.Theme = this.themeService.DialogTheme;
                view.ShowDialog(this);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs _)
        {
            Close();
        }

        private async void signoutToolStripMenuItem_Click(object sender, EventArgs _)
        {
            try
            {
                await this.viewModel.SignOutAsync().ConfigureAwait(true);

                var profile = this.serviceProvider.GetService<UserProfile>();
                if (!profile.IsDefault)
                {
                    if (this.serviceProvider
                        .GetService<IConfirmationDialog>()
                        .Confirm(
                            this,
                            $"Would you like to keep the current profile '{profile.Name}'?",
                            "Keep profile",
                            "Sign out") == DialogResult.No)
                    {
                        //
                        // Delete current profile.
                        //
                        // Because of the single-instance behavior of this app, we know
                        // (with reasonable certainty) that this is the only instance 
                        // that's currently using this profile. Therefore, it's safe
                        // to perform the deletion here.
                        //
                        // If we provided a "Delete profile" option in the profile
                        // selection, we couldn't know for sure that the profile
                        // isn't currently being used by another instance.
                        //
                        UserProfile.DeleteProfile(
                            this.serviceProvider.GetService<IInstall>(),
                            profile.Name);

                        //
                        // Perform a hard exit to avoid touching the
                        // registry keys (which are now marked for deletion)
                        // again.
                        //
                        Environment.Exit(0);
                    }
                }

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
            this.serviceProvider
                .GetService<IToolWindowHost>()
                .GetToolWindow<ProjectExplorerView, ProjectExplorerViewModel>()
                .Show();
        }

        private void openIapDocsToolStripMenuItem_Click(object sender, EventArgs _)
        {
            this.serviceProvider.GetService<HelpClient>().OpenTopic(HelpTopics.IapOverview);
        }

        private void openSecureConnectDocsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.serviceProvider.GetService<HelpClient>().OpenTopic(HelpTopics.SecureConnectDcaOverview);
        }

        private void openIapAccessDocsToolStripMenuItem_Click(object sender, EventArgs _)
        {
            this.serviceProvider.GetService<HelpClient>().OpenTopic(HelpTopics.IapAccess);
        }

        private void openIapFirewallDocsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.serviceProvider.GetService<HelpClient>().OpenTopic(HelpTopics.CreateIapFirewallRule);
        }

        private void reportGithubIssueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.serviceProvider.GetService<BugReportClient>().ReportBug(new BugReport());
        }

        private void viewHelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.serviceProvider.GetService<HelpClient>().OpenTopic(HelpTopics.General);
        }

        private void viewShortcutsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.serviceProvider.GetService<HelpClient>().OpenTopic(HelpTopics.Shortcuts);
        }

        private void releaseNotesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var window = this.serviceProvider
                .GetService<IToolWindowHost>()
                .GetToolWindow<ReleaseNotesView, ReleaseNotesViewModel>();
            window.Show();
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
                OptionsDialog.Show(this, (IServiceCategoryProvider)this.serviceProvider);
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

        private void accessStateButton_Click(object sender, EventArgs e)
        {
            var button = (ToolStripItem)sender;
            var screenPosition = new Rectangle(
                this.statusStrip.PointToScreen(button.Bounds.Location),
                button.Size);

            this.serviceProvider
                .GetWindow<AccessInfoFlyoutView, AccessInfoViewModel>(this.themeService.MainWindowTheme)
                .Form
                .Show(this, screenPosition, ContentAlignment.TopLeft);
        }

        private void addProfileToolStripMenuItem_Click(object sender, EventArgs _)
        {
            try
            {
                using (var dialog = this.serviceProvider
                    .GetDialog<NewProfileView, NewProfileViewModel>(this.themeService.DialogTheme))
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        using (var profile = UserProfile.CreateProfile(
                            this.serviceProvider.GetService<IInstall>(),
                            dialog.ViewModel.ProfileName))
                        {
                            this.viewModel.LaunchInstanceWithProfile(profile.Name);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                this.serviceProvider
                    .GetService<IExceptionDialog>()
                    .Show(this, "New profile", e);
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
                    //
                    // Show WaitDialog, blocking all user intraction.
                    //
                    var waitDialog = new WaitDialog(this, jobDescription.StatusMessage, cancellationSource);
                    this.themeService.DialogTheme.ApplyTo(waitDialog);
                    return waitDialog;

                default:
                    return this.viewModel.CreateBackgroundJob(jobDescription, cancellationSource);
            }
        }

        public void Reauthorize()
        {
            using (var dialog = this.serviceProvider
                .GetDialog<AuthorizeView, AuthorizeViewModel>(this.themeService.DialogTheme))
            {
                dialog.ViewModel.UseExistingAuthorization(
                    this.serviceProvider.GetService<IAuthorization>());
                dialog.ShowDialog(this);
            }
        }

        private void cancelBackgroundJobsButton_Click(object sender, EventArgs e)
            => this.viewModel.CancelBackgroundJobs();

        //---------------------------------------------------------------------
        // Helper classes.
        //---------------------------------------------------------------------

        private class CallbackSource<TContext> : IContextSource<TContext>
        {
            private readonly Func<TContext> queryCurrentContextFunc;

            public CallbackSource(Func<TContext> queryCurrentContextFunc)
            {
                this.queryCurrentContextFunc = queryCurrentContextFunc;
            }

            public TContext Context => this.queryCurrentContextFunc();
        }
    }
}
