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

using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Shell.Controls;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.Ssh;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal
{
    public partial class SshTerminalPane : ToolWindow, ISshTerminalSession
    {
        private readonly IExceptionDialog exceptionDialog;

        #pragma warning disable IDE0069 // Disposable fields should be disposed
        private readonly SshTerminalPaneViewModel viewModel;
        #pragma warning restore IDE0069 // Disposable fields should be disposed

        //---------------------------------------------------------------------
        // Properties.
        //---------------------------------------------------------------------

        public VirtualTerminal Terminal => this.terminal;

        public InstanceLocator Instance => this.viewModel.Instance;

        public bool IsFormClosing { get; private set; } = false;

        public override string Text
        {
            get => this.viewModel?.Instance?.Name ?? "SSH";
            set { }
        }

        //---------------------------------------------------------------------
        // Statics.
        //---------------------------------------------------------------------

        public static SshTerminalPane TryGetExistingPane(
            IMainForm mainForm,
            InstanceLocator vmInstance)
        {
            return mainForm.MainPanel
                .Documents
                .EnsureNotNull()
                .OfType<SshTerminalPane>()
                .Where(pane => pane.Instance == vmInstance && !pane.IsFormClosing)
                .FirstOrDefault();
        }

        public static SshTerminalPane TryGetActivePane(
            IMainForm mainForm)
        {
            return mainForm.MainPanel.ActiveDocument as SshTerminalPane;
        }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        internal SshTerminalPane(
            IServiceProvider serviceProvider,
            InstanceLocator vmInstance,
            IPEndPoint endpoint,
            AuthorizedKey authorizedKey,
            CultureInfo language,
            TimeSpan connectionTimeout)
            : base(serviceProvider, DockState.Document)
        {
            InitializeComponent();

            this.exceptionDialog = serviceProvider.GetService<IExceptionDialog>();
            this.viewModel = new SshTerminalPaneViewModel(
                serviceProvider.GetService<IEventService>(),
                vmInstance,
                endpoint,
                authorizedKey,
                language,
                connectionTimeout)
            {
                View = this
            };

            Debug.Assert(this.viewModel.View != null);

            this.DockAreas = DockAreas.Document;

            //
            // Bind controls.
            //
            this.reconnectPanel.BindReadonlyProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsReconnectPanelVisible,
                this.components);
            this.spinner.BindReadonlyProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsSpinnerVisible,
                this.components);
            this.terminal.BindReadonlyProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsTerminalVisible,
                this.components);
            this.viewModel.OnPropertyChange(
                m => m.IsTerminalVisible,
                visible =>
                {
                    if (visible)
                    {
                        this.terminal.Focus();
                    }
                });
            
            Debug.Assert(this.Text != this.Name);

            //
            // Terminal I/O.
            //
            this.Terminal.SendData += (sender, args) =>
            {
                //
                // Relay user input to SSH connection.
                //
                // NB. This method will never throw an exception, so it is ok
                // to fire-and-forget it.
                //
                OnInputReceivedFromUserAsync(args.Data)
                    .ContinueWith(_ => { });
            };

            this.Terminal.TerminalResized += (sender, args) =>
            {
                //
                // NB. This method will never throw an exception, so it is ok
                // to fire-and-forget it.
                //
                OnTerminalResizedByUser()
                    .ContinueWith(_ => { });
            };

            this.Terminal.WindowTitleChanged += (sender, args) =>
            {
                this.TabText = this.Terminal.WindowTitle;
            };

            this.viewModel.ConnectionFailed += OnErrorReceivedFromServerAsync;
            this.viewModel.DataReceived += OnDataReceivedFromServerAsync;

            //
            // Apply Terminal settings.
            //
            // Automatically reapply when settings change.
            //
            var terminalSettingsRepository = serviceProvider.GetService<TerminalSettingsRepository>();
            var settings = terminalSettingsRepository.GetSettings();
            ApplyTerminalSettings(settings);

            EventHandler<EventArgs<TerminalSettings>> reapplyTerminalSettings = 
                (s, e) => ApplyTerminalSettings(e.Data);
            terminalSettingsRepository.SettingsChanged += reapplyTerminalSettings;

            //
            // Disposing.
            //
            this.Disposed += (sender, args) =>
            {
                if (!this.IsDisposed)
                {
                    this.viewModel.ConnectionFailed -= OnErrorReceivedFromServerAsync;
                    this.viewModel.DataReceived -= OnDataReceivedFromServerAsync;
                    this.viewModel.Dispose();

                    terminalSettingsRepository.SettingsChanged -= reapplyTerminalSettings;
                }
            };
            this.FormClosed += OnFormClosed;
#if DEBUG
            var copyStream = new ToolStripMenuItem("DEBUG: Copy received data");
            copyStream.Click += (sender, args) => this.viewModel.CopyReceivedDataToClipboard();
            this.TabContextStrip.Items.Add(copyStream);
#endif
        }

        private void ApplyTerminalSettings(TerminalSettings settings)
        {
            this.terminal.EnableCtrlC = settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.BoolValue;
            this.terminal.EnableCtrlV = settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.BoolValue;

            this.terminal.EnableCtrlInsert = settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.BoolValue;
            this.terminal.EnableShiftInsert = settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.BoolValue;

            this.terminal.EnableTypographicQuoteConversionOnPaste = settings.IsQuoteConvertionOnPasteEnabled.BoolValue;

            this.terminal.EnableCtrlA = settings.IsSelectAllUsingCtrlAEnabled.BoolValue;
            this.terminal.EnableShiftLeftRight = settings.IsSelectUsingShiftArrrowEnabled.BoolValue;
            this.terminal.EnableShiftUpDown = settings.IsSelectUsingShiftArrrowEnabled.BoolValue;

            this.terminal.EnableCtrlLeftRight = settings.IsNavigationUsingControlArrrowEnabled.BoolValue;

            this.terminal.EnableCtrlUpDown = settings.IsScrollingUsingCtrlUpDownEnabled.BoolValue;
            this.terminal.EnableCtrlHomeEnd = settings.IsScrollingUsingCtrlHomeEndEnabled.BoolValue;
        }

        //---------------------------------------------------------------------
        // Event handlers
        //---------------------------------------------------------------------

        private void OnLayout(object sender, LayoutEventArgs e)
        {
            this.spinner.Location = new Point(
                (this.Size.Width - this.spinner.Width) / 2,
                (this.Size.Height - this.spinner.Height) / 2);

            this.reconnectPanel.Location = new Point(
                (this.Size.Width - this.reconnectPanel.Width) / 2,
                (this.Size.Height - this.reconnectPanel.Height) / 2);
        }

        private void ShowErrorAndClose(string caption, Exception e)
        {
            Debug.Assert(!this.InvokeRequired);

            this.exceptionDialog.Show(this, caption, e);
            Close();
        }

        private void OnFormClosed(object sender, System.Windows.Forms.FormClosedEventArgs e)
        {
            // Mark this pane as being in closing state even though it is still
            // visible at this point. The flag ensures that this pane is
            // not considered by TryGetExistingPane anymore.
            this.IsFormClosing = true;

            this.viewModel.DisconnectAsync().Wait();
        }

        private async Task OnInputReceivedFromUserAsync(string input)
        {
            Debug.Assert(!this.InvokeRequired);

            try
            {
                await this.viewModel.SendAsync(input)
                    .ConfigureAwait(true);
            }
            catch (Exception e)
            {
                ShowErrorAndClose("Sending data failed", e);
            }
        }

        private async Task OnTerminalResizedByUser()
        {
            Debug.Assert(!this.InvokeRequired);

            try
            {
                await this.viewModel.ResizeTerminal(
                    new TerminalSize(
                        (ushort)this.Terminal.Columns,
                        (ushort)this.Terminal.Rows))
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                ShowErrorAndClose("Sending data failed", e);
            }
        }

        private void OnDataReceivedFromServerAsync(
            object sender,
            DataReceivedEventArgs args)
        {
            Debug.Assert(!this.InvokeRequired);

            if (args.Data.Length == 0)
            {
                // End of stream -> close the pane.
                Close();
            }
            else
            {
                this.Terminal.ReceiveData(args.Data);
            }
        }

        private void OnErrorReceivedFromServerAsync(
            object sender,
            ConnectionErrorEventArgs args)
        {
            Debug.Assert(!this.InvokeRequired);
            ShowErrorAndClose("SSH connection terminated", args.Error);
        }

        private async void OnReconnectLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            await ConnectAsync()
                .ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // Actions
        //---------------------------------------------------------------------

        public async Task ConnectAsync()
        {
            try
            {
                await this.viewModel.ConnectAsync(
                        new TerminalSize(
                            (ushort)this.terminal.Columns,
                            (ushort)this.terminal.Rows))
                    .ConfigureAwait(true);
            }
            catch (Exception e)
            {
                ShowErrorAndClose("Connection failed", e);
            }
        }

        public Task SendAsync(string command) 
            => this.viewModel.SendAsync(command);

        public Task DisconnectAsync() 
            => this.viewModel.DisconnectAsync();
    }
}
