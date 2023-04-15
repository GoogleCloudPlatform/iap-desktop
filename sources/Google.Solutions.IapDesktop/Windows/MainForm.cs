﻿//
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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Interop;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.About;
using Google.Solutions.IapDesktop.Application.Views.Authorization;
using Google.Solutions.IapDesktop.Application.Views.Diagnostics;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Application.Views.Options;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Mvvm.Drawing;
using Google.Solutions.Mvvm.Shell;
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
        private readonly ApplicationSettingsRepository applicationSettings;
        private readonly IServiceProvider serviceProvider;
        private readonly IBindingContext bindingContext;

        private readonly ContextSource<IMainWindow> viewMenuContextSource;
        private readonly ContextSource<ToolWindow> windowMenuContextSource;

        private readonly CommandContainer<IMainWindow> viewMenuCommands;
        private readonly CommandContainer<ToolWindow> windowMenuCommands;

        public IapRdpUrl StartupUrl { get; set; }
        public ICommandContainer<IMainWindow> ViewMenu => this.viewMenuCommands;
        public ICommandContainer<ToolWindow> WindowMenu => this.windowMenuCommands;

        public MainForm(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            this.themeService = this.serviceProvider.GetService<IThemeService>();
            this.applicationSettings = this.serviceProvider.GetService<ApplicationSettingsRepository>();
            this.bindingContext = serviceProvider.GetService<IBindingContext>();

            //
            // Register this window with the binding context so that any
            // error dialos can use this window as owner.
            //
            ((ViewBindingContext)this.bindingContext).SetCurrentMainWindow(this);

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
                bindingContext);
            this.viewMenuCommands.BindTo(
                this.viewToolStripMenuItem,
                this.bindingContext);

            //
            // Window menu.
            //
            this.windowMenuContextSource = new ContextSource<ToolWindow>();

            this.windowToolStripMenuItem.DropDownOpening += (sender, args) =>
            {
                this.windowMenuContextSource.Context = this.dockPanel.ActiveContent as ToolWindow;
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
                        as ToolWindow;
            };

            this.windowMenuCommands = new CommandContainer<ToolWindow>(
                ToolStripItemDisplayStyle.ImageAndText,
                this.windowMenuContextSource,
                bindingContext);
            this.windowMenuCommands.BindTo(
                this.windowToolStripMenuItem,
                this.bindingContext);

            //
            // Bind controls.
            //
            this.viewModel = new MainFormViewModel(
                this,
                this.serviceProvider.GetService<IInstall>(),
                this.serviceProvider.GetService<Profile>(),
                this.serviceProvider.GetService<IAuthorization>(),
                this.themeService);

            this.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.WindowTitle,
                bindingContext);
            this.reportInternalIssueToolStripMenuItem.BindProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsReportInternalIssueVisible,
                bindingContext);

            //
            // Status bar.
            //
            this.statusStrip.BindProperty(
                c => c.BackColor,
                this.viewModel,
                m => m.StatusBarBackColor,
                bindingContext);
            this.toolStripStatus.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.StatusText,
                bindingContext);
            this.backgroundJobLabel.BindProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsBackgroundJobStatusVisible,
                bindingContext);
            this.cancelBackgroundJobsButton.BindProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsBackgroundJobStatusVisible,
                bindingContext);
            this.backgroundJobLabel.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.BackgroundJobStatus,
                bindingContext);
            this.deviceStateButton.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.DeviceStateCaption,
                bindingContext);
            this.deviceStateButton.BindProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsDeviceStateVisible,
                bindingContext);
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
                bindingContext);

            //
            // Bind menu commands.
            //
            this.WindowMenu.AddCommand(
                new ContextCommand<ToolWindow>(
                    "&Close",
                    window => window != null && window.IsDockable
                        ? CommandState.Enabled
                        : CommandState.Disabled,
                    window => window.CloseSafely()));
            this.WindowMenu.AddCommand(
                new ContextCommand<ToolWindow>(
                    "&Float",
                    window => window != null &&
                              !window.IsFloat &&
                              window.IsDockStateValid(DockState.Float)
                        ? CommandState.Enabled
                        : CommandState.Disabled,
                    window => window.IsFloat = true));
            this.WindowMenu.AddCommand(
                new ContextCommand<ToolWindow>(
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
                new ContextCommand<ToolWindow>(
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

            CommandState showTabCommand(ToolWindow window)
                => window != null && window.DockState == DockState.Document && window.Pane.Contents.Count > 1
                    ? CommandState.Enabled
                    : CommandState.Disabled;

            this.WindowMenu.AddCommand(
                new ContextCommand<ToolWindow>(
                    "&Next tab",
                    showTabCommand,
                    window => SwitchTab(window, 1))
                {
                    ShortcutKeys = Keys.Control | Keys.Alt | Keys.PageDown
                });
            this.WindowMenu.AddCommand(
                new ContextCommand<ToolWindow>(
                    "&Previous tab",
                    showTabCommand,
                    window => SwitchTab(window, -1))
                {
                    ShortcutKeys = Keys.Control | Keys.Alt | Keys.PageUp
                });
            this.WindowMenu.AddCommand(
                new ContextCommand<ToolWindow>(
                    "Capture/release &focus",
                    _ => this.dockPanel.ActiveDocumentPane != null &&
                         this.dockPanel.ActiveDocumentPane.Contents.EnsureNotNull().Any()
                        ? CommandState.Enabled
                        : CommandState.Disabled,
                    window => (this.dockPanel.ActiveDocumentPane?.ActiveContent as DocumentWindow)?.SwitchToDocument())
                {
                    ShortcutKeys = Keys.Control | Keys.Alt | Keys.Home
                });

#if DEBUG
            var debugCommand = this.ViewMenu.AddCommand(
                new ContextCommand<IMainWindow>(
                    "Debug",
                    _ => CommandState.Enabled,
                    context => { }));
            debugCommand.AddCommand(new ContextCommand<IMainWindow>(
                "Job Service",
                _ => CommandState.Enabled,
                _ => ToolWindow
                    .GetWindow<DebugJobServiceView, DebugJobServiceViewModel>(this.serviceProvider)
                    .Show()));
            debugCommand.AddCommand(new ContextCommand<IMainWindow>(
                "Docking ",
                _ => CommandState.Enabled,
                _ => ToolWindow
                    .GetWindow<DebugDockingView, DebugDockingViewModel>(this.serviceProvider)
                    .Show()));
            debugCommand.AddCommand(new ContextCommand<IMainWindow>(
                "Project Explorer Tracking",
                _ => CommandState.Enabled,
                _ => ToolWindow
                    .GetWindow<DebugProjectExplorerTrackingView, DebugProjectExplorerTrackingViewModel>(this.serviceProvider)
                    .Show()));
            debugCommand.AddCommand(new ContextCommand<IMainWindow>(
                "Full screen pane",
                _ => CommandState.Enabled,
                _ => ToolWindow
                    .GetWindow<DebugFullScreenView, DebugFullScreenViewModel>(this.serviceProvider)
                    .Show()));
            debugCommand.AddCommand(new ContextCommand<IMainWindow>(
                "Theme",
                _ => CommandState.Enabled,
                _ => ToolWindow
                    .GetWindow<DebugThemeView, DebugThemeViewModel>(this.serviceProvider)
                    .Show()));
            debugCommand.AddCommand(new ContextCommand<IMainWindow>(
                "Registered services",
                _ => CommandState.Enabled,
                _ => ToolWindow
                    .GetWindow<DebugServiceRegistryView, DebugServiceRegistryViewModel>(this.serviceProvider)
                    .Show()));
            debugCommand.AddCommand(new ContextCommand<IMainWindow>(
                "Common controls",
                _ => CommandState.Enabled,
                _ => ToolWindow
                    .GetWindow<DebugCommonControlsView, DebugCommonControlsViewModel>(this.serviceProvider)
                    .Show()));

            var crashCommand = debugCommand.AddCommand(new ContextCommand<IMainWindow>(
                "Exceptions",
                _ => CommandState.Enabled,
                _ => { }));
            crashCommand.AddCommand(new ContextCommand<IMainWindow>(
                "Command: Throw ExceptionWithHelp (sync)",
                _ => CommandState.Enabled,
                _ => throw new ResourceAccessDeniedException(
                        "DEBUG",
                        HelpTopics.General,
                        new ApplicationException("DEBUG"))));
            crashCommand.AddCommand(new ContextCommand<IMainWindow>(
                "Command: Throw ApplicationException (sync)",
                _ => CommandState.Enabled,
                _ => throw new ApplicationException("DEBUG")));
            crashCommand.AddCommand(new ContextCommand<IMainWindow>(
                "Command: Throw ApplicationException (async)",
                _ => CommandState.Enabled,
                async _ =>
                {
                    await Task.Yield();
                    throw new ApplicationException("DEBUG");
                }));
            crashCommand.AddCommand(new ContextCommand<IMainWindow>(
                "Command: Throw TaskCanceledException (sync)",
                _ => CommandState.Enabled,
                _ => throw new TaskCanceledException("DEBUG")));
            crashCommand.AddCommand(new ContextCommand<IMainWindow>(
                "Command: Throw TaskCanceledException (async)",
                _ => CommandState.Enabled,
                async _ =>
                {
                    await Task.Yield();
                    throw new TaskCanceledException("DEBUG");
                }));
            crashCommand.AddCommand(new ContextCommand<IMainWindow>(
                "Window: Throw ApplicationException",
                _ => CommandState.Enabled,
                _ =>
                {
                    this.BeginInvoke((Action)(() => throw new ApplicationException("DEBUG")));
                }));
#endif

            ResumeLayout();
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            var settings = this.applicationSettings.GetSettings();

            var updateService = this.serviceProvider.GetService<IUpdateService>();
            if (settings.IsUpdateCheckEnabled.BoolValue &&
                updateService.IsUpdateCheckDue(DateTime.FromBinary(settings.LastUpdateCheck.LongValue)))
            {
                //
                // Time to check for updates again.
                //
                try
                {
                    updateService.CheckForUpdates(
                        this,
                        out bool donotCheckForUpdatesAgain);

                    settings.IsUpdateCheckEnabled.BoolValue = !donotCheckForUpdatesAgain;
                    settings.LastUpdateCheck.LongValue = DateTime.UtcNow.ToBinary();
                }
                catch (Exception)
                {
                    // Nevermind.
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
            var profile = this.serviceProvider.GetService<Profile>();
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
                // Dispatch URL.
                ConnectToUrl(this.StartupUrl);
            }
            else
            {
                // No startup URL provided, just show project explorer then.
                ToolWindow
                    .GetWindow<ProjectExplorerView, ProjectExplorerViewModel>(this.serviceProvider)
                    .Show();
            }
        }

        private void SwitchTab(ToolWindow reference, int delta)
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

        private ContextCommand<ToolWindow> CreateDockCommand(
            string caption,
            DockState dockState,
            Keys shortcutKeys)
        {
            return new ContextCommand<ToolWindow>(
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
            if (this.dockPanel.ActiveContent is ToolWindow toolWindow &&
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
                await this.viewModel.RevokeAuthorizationAsync().ConfigureAwait(true);

                var profile = this.serviceProvider.GetService<Profile>();
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
                        Profile.DeleteProfile(
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
            ToolWindow
                .GetWindow<ProjectExplorerView, ProjectExplorerViewModel>(this.serviceProvider)
                .Show();
        }

        private void openIapDocsToolStripMenuItem_Click(object sender, EventArgs _)
        {
            this.serviceProvider.GetService<HelpAdapter>().OpenTopic(HelpTopics.IapOverview);
        }

        private void openSecureConnectDocsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.serviceProvider.GetService<HelpAdapter>().OpenTopic(HelpTopics.SecureConnectDcaOverview);
        }

        private void openIapAccessDocsToolStripMenuItem_Click(object sender, EventArgs _)
        {
            this.serviceProvider.GetService<HelpAdapter>().OpenTopic(HelpTopics.IapAccess);
        }

        private void openIapFirewallDocsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.serviceProvider.GetService<HelpAdapter>().OpenTopic(HelpTopics.CreateIapFirewallRule);
        }

        private void reportGithubIssueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.serviceProvider.GetService<BuganizerAdapter>().ReportBug(new BugReport());
        }

        private void reportInternalIssueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.serviceProvider.GetService<BuganizerAdapter>().ReportPrivateBug(new BugReport());
        }

        private void viewHelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.serviceProvider.GetService<HelpAdapter>().OpenTopic(HelpTopics.General);
        }

        private void viewShortcutsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.serviceProvider.GetService<HelpAdapter>().OpenTopic(HelpTopics.Shortcuts);
        }
        private void releaseNotesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.serviceProvider.GetService<HelpAdapter>().OpenTopic(HelpTopics.ReleaseNotes);
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

        private void toolStripDeviceStateButton_Click(object sender, EventArgs e)
        {
            var button = (ToolStripItem)sender;
            var screenPosition = new Rectangle(
                this.statusStrip.PointToScreen(button.Bounds.Location),
                button.Size);

            this.serviceProvider
                .GetWindow<DeviceFlyoutView, DeviceFlyoutViewModel>(this.themeService.MainWindowTheme)
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
                        using (var profile = Profile.CreateProfile(
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
