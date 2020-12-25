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
using Google.Solutions.IapDesktop.Application.Controls;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Native;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Views.Terminal
{
    public class SshTerminalPaneViewModel : ViewModelBase, IDisposable
    {
        private readonly IEventService eventService;
        private readonly string username;
        private readonly IPEndPoint endpoint;
        private readonly ISshKey key;

        private Status connectionStatus = Status.Disconnected;
        private SshShellConnection currentConnection = null;

#if DEBUG
        private readonly StringBuilder receivedData = new StringBuilder();
#endif

        public InstanceLocator Instance { get; }

        public EventHandler<ConnectionFailedEventArgs> ConnectionFailed;
        public EventHandler<DataReceivedEventArgs> DataReceived;

        private ISynchronizeInvoke ViewInvoker => (ISynchronizeInvoke)this.View;

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        public enum Status
        {
            Connecting,
            Connected,
            Disconnected
        }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public SshTerminalPaneViewModel(
            IEventService eventService,
            InstanceLocator vmInstance,
            string username,
            IPEndPoint endpoint,
            ISshKey key)
        {
            this.eventService = eventService;
            this.username = username;
            this.endpoint = endpoint;
            this.key = key;
            this.Instance = vmInstance;

        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public Status ConnectionStatus 
        {
            get => this.connectionStatus;
            private set
            {
                this.connectionStatus = value;

                RaisePropertyChange();
                RaisePropertyChange((SshTerminalPaneViewModel m) => m.IsSpinnerVisible);
                RaisePropertyChange((SshTerminalPaneViewModel m) => m.IsTerminalVisible);
            }
        }

        public bool IsSpinnerVisible => this.ConnectionStatus == Status.Connecting;
        public bool IsTerminalVisible => this.ConnectionStatus == Status.Connected;

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public async Task ConnectAsync(TerminalSize initialSize)
        {
            void OnErrorReceivedFromServerAsync(Exception exception)
            {
                // NB. Callback runs on SSH thread, not on UI thread.

                this.ViewInvoker.InvokeAndForget(
                    () => this.ConnectionFailed?.Invoke(
                        this,
                        new ConnectionFailedEventArgs(exception)));

                // Notify listeners.
                this.eventService.FireAsync(
                    new ConnectionFailedEvent(this.Instance, exception))
                    .ContinueWith(_ => { });
            }

            void OnDataReceivedFromServerAsync(string data)
            {
                // NB. Callback runs on SSH thread, not on UI thread.

#if DEBUG
                this.receivedData.Append(data);
#endif

                this.ViewInvoker.InvokeAndForget(
                    () => this.DataReceived?.Invoke(
                        this,
                        new DataReceivedEventArgs(data)));
            }

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
                    this.ConnectionStatus = Status.Connecting;
                    this.currentConnection = new SshShellConnection(
                        this.username,
                        this.endpoint,
                        this.key,
                        SshShellConnection.DefaultTerminal,
                        initialSize,
                        CultureInfo.CurrentUICulture,
                        OnDataReceivedFromServerAsync,
                        OnErrorReceivedFromServerAsync)
                    {
                        Banner = SshSession.BannerPrefix + Globals.UserAgent
                    };

                    await this.currentConnection.ConnectAsync()
                        .ConfigureAwait(true);

                    this.ConnectionStatus = Status.Connected;

                    // Notify listeners.
                    await this.eventService.FireAsync(
                        new ConnectionSuceededEvent(this.Instance))
                        .ConfigureAwait(true);
                }
                catch (Exception e)
                {
                    this.ConnectionStatus = Status.Disconnected;
                    this.ConnectionFailed?.Invoke(
                        this,
                        new ConnectionFailedEventArgs(e));

                    // Notify listeners.
                    await this.eventService.FireAsync(
                        new ConnectionFailedEvent(this.Instance, e))
                        .ConfigureAwait(true);

                    this.currentConnection = null;
                }
            }
        }

        public async Task DisconnectAsync()
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

        public async Task SendAsync(string command)
        {
            if (this.currentConnection != null)
            {
                await this.currentConnection.SendAsync(command)
                    .ConfigureAwait(false);
            }
        }


        public async Task ResizeTerminal(TerminalSize newSize)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(newSize))
            {
                if (this.currentConnection != null)
                {
                    await this.currentConnection.ResizeTerminalAsync(newSize)
                        .ConfigureAwait(false);
                }
            }
        }

        public void CopyReceivedDataToClipboard()
        {
            if (this.receivedData.Length > 0)
            {
                Clipboard.SetText(this.receivedData.ToString());
            }
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.currentConnection?.Dispose();
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }



    public class DataReceivedEventArgs
    {
        public string Data { get; }

        public DataReceivedEventArgs(string data)
        {
            this.Data = data;
        }
    }

    public class ConnectionFailedEventArgs
    {
        public Exception Error { get; }

        public ConnectionFailedEventArgs(Exception error)
        {
            this.Error = error;
        }
    }
}
