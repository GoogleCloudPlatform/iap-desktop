//
// Copyright 2022 Google LLC
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

using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Ssh;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

#if !DEBUG
using Google.Solutions.IapDesktop.Application;
#endif

#nullable disable

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Ssh
{

    public abstract class TerminalViewModelBase : ViewModelBase, IDisposable
    {
        private readonly IEventQueue eventService;

        private Status connectionStatus = Status.ConnectionFailed;

        private readonly StringBuilder receivedData = new StringBuilder();
        private readonly StringBuilder sentData = new StringBuilder();

        public event EventHandler<ConnectionErrorEventArgs> ConnectionFailed;
        public event EventHandler<ConnectionErrorEventArgs> ConnectionLost;
        public event EventHandler<DataEventArgs> DataReceived;

        protected ISynchronizeInvoke ViewInvoker => (ISynchronizeInvoke)this.View;

        protected TerminalViewModelBase(IEventQueue eventService)
        {
            this.eventService = eventService.ExpectNotNull(nameof(eventService));
        }

        //---------------------------------------------------------------------
        // Initialization properties.
        //---------------------------------------------------------------------

        public InstanceLocator Instance { get; set; }

        protected override void OnValidate()
        {
            this.Instance.ExpectNotNull(nameof(this.Instance));
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        public enum Status
        {
            Connecting,
            Connected,
            ConnectionFailed,
            ConnectionLost
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

                Debug.Assert(this.ViewInvoker != null);
                if (this.ViewInvoker != null)
                {
                    //
                    // It's (unlikely, but) possible that the View has already been torn down.
                    // In that case, do not deliver events since they are likely to
                    // cause trouble and touch disposed objects.
                    //
                    Debug.Assert(!this.ViewInvoker.InvokeRequired, "Accessed from UI thread");

                    RaisePropertyChange();
                    RaisePropertyChange((SshTerminalViewModel m) => m.IsSpinnerVisible);
                    RaisePropertyChange((SshTerminalViewModel m) => m.IsTerminalVisible);
                    RaisePropertyChange((SshTerminalViewModel m) => m.IsReconnectPanelVisible);
                }
            }
        }

        public bool IsSpinnerVisible => this.ConnectionStatus == Status.Connecting;
        public bool IsTerminalVisible => this.ConnectionStatus == Status.Connected;
        public bool IsReconnectPanelVisible => this.ConnectionStatus == Status.ConnectionLost;


        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------
        public abstract Task ConnectAsync(TerminalSize initialSize);

        public abstract Task DisconnectAsync();

        public abstract Task SendAsync(string command);

        public abstract Task ResizeTerminal(TerminalSize newSize);


        public void CopySentDataToClipboard()
        {
            if (this.sentData.Length > 0)
            {
                ClipboardUtil.SetText(this.sentData.ToString());
            }
        }

        public void CopyReceivedDataToClipboard()
        {
            if (this.receivedData.Length > 0)
            {
                ClipboardUtil.SetText(this.receivedData.ToString());
            }
        }

        //---------------------------------------------------------------------
        // Events.
        //---------------------------------------------------------------------

        protected async Task OnConnectionLost(ConnectionErrorEventArgs args)
        {
            this.ConnectionStatus = Status.ConnectionLost;
            this.ConnectionLost?.Invoke(this, args);

            await this.eventService
                .PublishAsync(new SessionAbortedEvent(this.Instance, args.Error))
                .ConfigureAwait(true);
        }

        protected async Task OnConnectionFailed(ConnectionErrorEventArgs args)
        {
            this.ConnectionStatus = Status.ConnectionFailed;
            this.ConnectionFailed?.Invoke(this, args);

            await this.eventService
                .PublishAsync(new SessionAbortedEvent(this.Instance, args.Error))
                .ConfigureAwait(true);
        }

        protected Task OnBeginConnect()
        {
            this.ConnectionStatus = Status.Connecting;
            return Task.CompletedTask;
        }

        protected async Task OnConnected()
        {
            this.ConnectionStatus = Status.Connected;

            // Notify listeners.
            await this.eventService
                .PublishAsync(new SessionStartedEvent(this.Instance))
                .ConfigureAwait(true);
        }

        protected async Task OnDisconnected()
        {
            await this.eventService
                .PublishAsync(new SessionEndedEvent(this.Instance))
                .ConfigureAwait(true);
        }

        protected void OnDataReceived(DataEventArgs args)
        {
            // Keep buffer if DEBUG or tracing enabled.
#if !DEBUG
            if (ApplicationTraceSource.Log.Switch.ShouldTrace(TraceEventType.Verbose))
#endif
            {
                this.receivedData.Append(args.Data);
            }

            this.DataReceived?.Invoke(this, args);
        }

        protected void OnDataSent(DataEventArgs args)
        {
            // Keep buffer if DEBUG or tracing enabled.
#if !DEBUG
            if (ApplicationTraceSource.Log.Switch.ShouldTrace(TraceEventType.Verbose))
#endif
            {
                this.sentData.Append(args.Data);
            }
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                // 
                // Do not invoke any more view callbacks since they can lead to
                // a deadlock: If the connection is being disposed, odds are
                // that the window (ViewInvoker) has already been destructed.
                // That could cause InvokeAndForget to hang, causing a deadlock
                // between the UI thread and the SSH worker thread.
                //
                Debug.Assert(this.View == null);
            }
        }
    }

    public class DataEventArgs
    {
        public string Data { get; }

        public DataEventArgs(string data)
        {
            this.Data = data;
        }
    }

    public class ConnectionErrorEventArgs
    {
        public Exception Error { get; }

        public ConnectionErrorEventArgs(Exception error)
        {
            this.Error = error;
        }
    }

}
