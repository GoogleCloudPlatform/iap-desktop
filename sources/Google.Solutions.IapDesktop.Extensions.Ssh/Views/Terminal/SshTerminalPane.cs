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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Ssh.Controls;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Native;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Views.Terminal
{
    public partial class SshTerminalPane : ToolWindow, ISshTerminalPane
    {
        private readonly IExceptionDialog exceptionDialog;
        private readonly IEventService eventService;

        private readonly string username;
        private readonly IPEndPoint endpoint;
        private readonly ISshKey key;

        private SshShellConnection currentConnection = null;

#if DEBUG
        private readonly StringBuilder receivedData = new StringBuilder();
#endif
        //---------------------------------------------------------------------
        // Properties.
        //---------------------------------------------------------------------

        public InstanceLocator Instance { get; }

        public VirtualTerminal Terminal => this.terminal;

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
            this.eventService = serviceProvider.GetService<IEventService>();
            this.username = username;
            this.endpoint = endpoint;
            this.key = key;
            this.Instance = vmInstance;
            this.Text = vmInstance.Name;
            this.DockAreas = DockAreas.Document;

            Debug.Assert(this.Text != this.Name);

            this.Disposed += OnDisposed;
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
                    .ConfigureAwait(false);
            };

            this.Terminal.TerminalResized += (sender, args) =>
            {
                //
                // NB. This method will never throw an exception, so it is ok
                // to fire-and-forget it.
                //
                OnTerminalResizedByUser()
                    .ConfigureAwait(false);
            };

            this.Terminal.WindowTitleChanged += (sender, args) =>
            {
                this.TabText = this.Terminal.WindowTitle;
            };

#if DEBUG
            var copyStream = new ToolStripMenuItem("DEBUG: Copy received data");
            copyStream.Click += (sender, args) =>
            {
                if (this.receivedData.Length > 0)
                {
                    Clipboard.SetText(this.receivedData.ToString());
                }
            };
            this.TabContextStrip.Items.Add(copyStream);
#endif
        }

        private void OnFormClosed(object sender, System.Windows.Forms.FormClosedEventArgs e)
        {
            DisconnectAsync().Wait();
        }

        private void OnDisposed(object sender, EventArgs e)
        {
            this.currentConnection?.Dispose();
        }

        //---------------------------------------------------------------------
        // Protected.
        //---------------------------------------------------------------------

        protected async Task ShowErrorAndCloseAsync(string caption, Exception e)
        {
            void ShowErrorAndCloseUnsafe()
            {
                Debug.Assert(!this.InvokeRequired);

                this.exceptionDialog.Show(this, caption, e);
                Close();
            }

            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(e.Message))
            {
                // Notify listeners.
                await this.eventService.FireAsync(
                    new ConnectionFailedEvent(this.Instance, e))
                    .ConfigureAwait(true);

                if (this.InvokeRequired)
                {
                    this.BeginInvoke((Action)ShowErrorAndCloseUnsafe);
                }
                else
                {
                    ShowErrorAndCloseUnsafe();
                }
            }
        }

        protected async Task DisconnectAsync()
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            {
                if (this.currentConnection != null)
                {
                    this.currentConnection.Dispose();
                    this.currentConnection = null;

                    // Notify listeners.
                    await this.eventService.FireAsync(
                        new ConnectionClosedEvent(this.Instance))
                        .ConfigureAwait(true);
                }
            }
        }

        //---------------------------------------------------------------------
        // Event handlers
        //---------------------------------------------------------------------

        private async Task ReconnectAsync()
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            {
                //
                // Disconnect previous session, if any.
                //
                await DisconnectAsync()
                    .ConfigureAwait(true);
                Debug.Assert(this.currentConnection == null);

                //
                // Establish a new connection and create a shell.
                //
                try
                {
                    this.currentConnection = new SshShellConnection(
                        this.username,
                        this.endpoint,
                        this.key,
                        SshShellConnection.DefaultTerminal,
                        new TerminalSize(
                            (ushort)this.terminal.Columns,
                            (ushort)this.terminal.Rows),
                        CultureInfo.CurrentUICulture,
                        OnDataReceivedFromServerAsync,
                        OnErrorReceivedFromServerAsync)
                    {
                        Banner = SshSession.BannerPrefix + Globals.UserAgent
                    };

                    await this.currentConnection.ConnectAsync()
                        .ConfigureAwait(true);

                    // Notify listeners.
                    await this.eventService.FireAsync(
                        new ConnectionSuceededEvent(this.Instance))
                        .ConfigureAwait(true);
                }
                catch (Exception e)
                {
                    this.currentConnection = null;
                    await ShowErrorAndCloseAsync("Connection failed", e)
                        .ConfigureAwait(false);
                }
            }
        }

        private async Task OnInputReceivedFromUserAsync(string input)
        {
            try
            {
                Debug.Assert(this.currentConnection != null);

                if (this.currentConnection != null)
                {
                    await this.currentConnection.SendAsync(input)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                await ShowErrorAndCloseAsync(
                        "Sending data failed",
                        e)
                    .ConfigureAwait(false);
            }
        }

        private async Task OnTerminalResizedByUser()
        {
            try
            {
                if (this.currentConnection != null)
                {
                   await this.currentConnection.ResizeTerminalAsync(
                       new TerminalSize(
                           (ushort)this.Terminal.Columns,
                           (ushort)this.Terminal.Rows))
                       .ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                await ShowErrorAndCloseAsync(
                        "Sending data failed",
                        e)
                    .ConfigureAwait(false);
            }
        }

        private void OnDataReceivedFromServerAsync(string data)
        {
            Debug.Assert(this.currentConnection != null);

#if DEBUG
            this.receivedData.Append(data);
#endif

            // NB. This callback might be invoked on a non-UI thread.
            this.BeginInvoke((Action)(() =>
            {
                if (data.Length == 0)
                {
                    // End of stream -> close the pane.
                    Close();
                }
                else
                {
                    this.Terminal.PushText(data);
                }
            }));
        }

        private void OnErrorReceivedFromServerAsync(Exception exception)
        {
            Debug.Assert(this.currentConnection != null);

            ShowErrorAndCloseAsync("SSH connection terminated", exception)
                .ContinueWith(_ => { });
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public Task ConnectShellAsync() => ReconnectAsync();

        public async Task SendAsync(string command)
        {
            if (this.currentConnection != null)
            {
                await this.currentConnection.SendAsync(command)
                    .ConfigureAwait(false);
            }
        }
    }
}
