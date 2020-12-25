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
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Ssh.Controls;
using Google.Solutions.Ssh;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Views.Terminal
{
    public partial class SshTerminalPane : ToolWindow, ISshTerminalPane
    {
        private readonly IExceptionDialog exceptionDialog;
        private readonly SshTerminalPaneViewModel viewModel;

        //---------------------------------------------------------------------
        // Properties.
        //---------------------------------------------------------------------

        public VirtualTerminal Terminal => this.terminal;

        public InstanceLocator Instance => this.viewModel.Instance;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        internal SshTerminalPane(
            IServiceProvider serviceProvider,
            InstanceLocator vmInstance,
            string username,
            IPEndPoint endpoint,
            ISshKey key)
            : base(serviceProvider, DockState.Document)
        {
            InitializeComponent();

            this.exceptionDialog = serviceProvider.GetService<IExceptionDialog>();
            this.viewModel = new SshTerminalPaneViewModel(
                serviceProvider.GetService<IEventService>(),
                vmInstance,
                username,
                endpoint,
                key);
            this.viewModel.View = this;

            this.Text = vmInstance.Name;
            this.DockAreas = DockAreas.Document;

            Debug.Assert(this.Text != this.Name);

            this.Disposed += (sender, args) =>
            {
                this.viewModel.Dispose();
            };
            this.FormClosed += OnFormClosed;
            this.Terminal.InputReceived += (sender, args) =>
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

#if DEBUG
            var copyStream = new ToolStripMenuItem("DEBUG: Copy received data");
            copyStream.Click += (sender, args) => this.viewModel.CopyReceivedDataToClipboard();
            this.TabContextStrip.Items.Add(copyStream);
#endif
        }

        //---------------------------------------------------------------------
        // Event handlers
        //---------------------------------------------------------------------

        private void ShowErrorAndClose(string caption, Exception e)
        {
            Debug.Assert(!this.InvokeRequired);

            this.exceptionDialog.Show(this, caption, e);
            Close();
        }

        private void OnFormClosed(object sender, System.Windows.Forms.FormClosedEventArgs e)
        {
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
                this.Terminal.PushText(args.Data);
            }
        }

        private void OnErrorReceivedFromServerAsync(
            object sender,
            ConnectionFailedEventArgs args)
        {
            Debug.Assert(!this.InvokeRequired);

            ShowErrorAndClose("SSH connection terminated", args.Error);
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
    }
}
