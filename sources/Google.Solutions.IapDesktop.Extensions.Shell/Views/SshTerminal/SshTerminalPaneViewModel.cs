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
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Controls;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Native;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal
{
    public class SshTerminalPaneViewModel : ViewModelBase, IDisposable
    {
        private readonly IEventService eventService;
        private readonly CultureInfo language;
        private readonly IPEndPoint endpoint;
        private readonly AuthorizedKey authorizedKey;
        private readonly TimeSpan connectionTimeout;

        private Status connectionStatus = Status.ConnectionFailed;
        private SshShellConnection currentConnection = null;

        private readonly StringBuilder receivedData = new StringBuilder();
        private readonly StringBuilder sentData = new StringBuilder();

        public InstanceLocator Instance { get; }

        public event EventHandler<ConnectionErrorEventArgs> ConnectionFailed;
        public event EventHandler<ConnectionErrorEventArgs> ConnectionLost;
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<AuthenticationPromptEventArgs> AuthenticationPrompt;

        private ISynchronizeInvoke ViewInvoker => (ISynchronizeInvoke)this.View;

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
        // Ctor.
        //---------------------------------------------------------------------

        public SshTerminalPaneViewModel(
            IEventService eventService,
            InstanceLocator vmInstance,
            IPEndPoint endpoint,
            AuthorizedKey authorizedKey,
            CultureInfo language,
            TimeSpan connectionTimeout)
        {
            this.eventService = eventService;
            this.endpoint = endpoint;
            this.authorizedKey = authorizedKey;
            this.language = language;
            this.Instance = vmInstance;
            this.connectionTimeout = connectionTimeout;
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
                    // It's (unlikely but( possible that the View has already been torn down.
                    // In that case, do not deliver events since they are likely to
                    // cause trouble and touch disposed objects.
                    //
                    Debug.Assert(!this.ViewInvoker.InvokeRequired, "Accessed from UI thread");

                    RaisePropertyChange();
                    RaisePropertyChange((SshTerminalPaneViewModel m) => m.IsSpinnerVisible);
                    RaisePropertyChange((SshTerminalPaneViewModel m) => m.IsTerminalVisible);
                    RaisePropertyChange((SshTerminalPaneViewModel m) => m.IsReconnectPanelVisible);
                }
            }
        }

        public bool IsSpinnerVisible => this.ConnectionStatus == Status.Connecting;
        public bool IsTerminalVisible => this.ConnectionStatus == Status.Connected;
        public bool IsReconnectPanelVisible => this.ConnectionStatus == Status.ConnectionLost;

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        private async Task ConnectAndTranslateErrorsAsync()
        {
            try
            {
                await this.currentConnection.ConnectAsync()
                    .ConfigureAwait(false);
            }
            catch (SshNativeException e) when (
                e.ErrorCode == LIBSSH2_ERROR.AUTHENTICATION_FAILED &&
                this.authorizedKey.AuthorizationMethod == AuthorizeKeyMethods.Oslogin)
            {
                throw new OsLoginAuthenticationFailedException(
                    "You do not have sufficient permissions to access this VM instance.\n\n" +
                    "To perform this action, you need the following roles (or an equivalent custom role):\n\n" +
                    " 1. 'Compute OS Login' or 'Compute OS Admin Login'\n" +
                    " 2. 'Service Account User' (if the VM uses a service account)\n" +
                    " 3. 'Compute OS Login External User' (if the VM belongs to a different GCP organization\n",
                    e,
                    HelpTopics.GrantingOsLoginRoles);
            }
        }

        public async Task ConnectAsync(TerminalSize initialSize)
        {
            Debug.Assert(this.View != null);
            Debug.Assert(this.ViewInvoker != null);

            void OnErrorReceivedFromServerAsync(Exception exception)
            {
                // NB. Callback runs on SSH thread, not on UI thread.
                ApplicationTraceSources.Default.TraceVerbose("Error received from server: {0}", exception);

                var errorsIndicatingLostConnection = new[]
                {
                    LIBSSH2_ERROR.SOCKET_SEND,
                    LIBSSH2_ERROR.SOCKET_RECV,
                    LIBSSH2_ERROR.SOCKET_TIMEOUT
                };

                if (this.ConnectionStatus == Status.Connected &&
                    exception.Unwrap() is SshNativeException sshEx &&
                    errorsIndicatingLostConnection.Contains(sshEx.ErrorCode))
                {
                    this.ViewInvoker?.InvokeAndForget(
                        () =>
                        {
                            this.ConnectionStatus = Status.ConnectionLost;
                            this.ConnectionLost?.Invoke(
                                this,
                                new ConnectionErrorEventArgs(exception));
                        });
                }
                else
                {
                    this.ViewInvoker?.InvokeAndForget(
                        () => this.ConnectionFailed?.Invoke(
                            this,
                            new ConnectionErrorEventArgs(exception)));
                }

                // Notify listeners.
                this.eventService.FireAsync(
                    new SessionAbortedEvent(this.Instance, exception))
                    .ContinueWith(_ => { });
            }

            void OnDataReceivedFromServerAsync(string data)
            {
                // NB. Callback runs on SSH thread, not on UI thread.

                ApplicationTraceSources.Default.TraceVerbose("Received {0} chars from server", data?.Length);

                RecordReceivedData(data);

                this.ViewInvoker?.InvokeAndForget(
                    () =>
                    {
                        try
                        {
                            this.DataReceived?.Invoke(
                                this,
                                new DataReceivedEventArgs(data));
                        }
                        catch (Exception e)
                        {
                            //
                            // A fatal error occured while processing data,
                            // possibly because of malformed or unsupported
                            // xterm data.
                            //
                            this.ConnectionFailed?.Invoke(
                                this,
                                new ConnectionErrorEventArgs(e));
                        }
                    });
            }

            string OnAuthenticationPrompt(
                string name,
                string instruction,
                string prompt,
                bool echo)
            {
                //
                // Trigger UI to respond to the prompt by firing an event.
                // As this method is invoked on a non-UI thread, switch to
                // the GUI thread first.
                //

                var args = new AuthenticationPromptEventArgs(prompt, !echo);
                this.ViewInvoker?.Invoke(
                    (Action)(() => 
                    {
                        this.AuthenticationPrompt?.Invoke(this, args);
                    }),
                    null);

                if (args.Response != null)
                {
                    //
                    // Strip:
                    //  - spaces between group of digits (g.co/sc)
                    //  - "G-" prefix (text messages)
                    //
                    if (args.Response.StartsWith("g-", StringComparison.OrdinalIgnoreCase))
                    {
                        args.Response = args.Response.Substring(2);
                    }

                    return args.Response.Replace(" ", string.Empty);
                }
                else
                {
                    return null;
                }
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
                        this.authorizedKey.Username,
                        this.endpoint,
                        this.authorizedKey.Key,
                        SshShellConnection.DefaultTerminal,
                        initialSize,
                        this.language,
                        OnAuthenticationPrompt,
                        OnDataReceivedFromServerAsync,
                        OnErrorReceivedFromServerAsync)
                    {
                        Banner = SshSession.BannerPrefix + Globals.UserAgent,
                        ConnectionTimeout = this.connectionTimeout,

                        //
                        // NB. Do not join worker thread as this could block the
                        // UI thread.
                        //
                        JoinWorkerThreadOnDispose = false
                    };

                    await ConnectAndTranslateErrorsAsync().ConfigureAwait(true);

                    this.ConnectionStatus = Status.Connected;

                    // Notify listeners.
                    await this.eventService.FireAsync(
                        new SessionStartedEvent(this.Instance))
                        .ConfigureAwait(true);
                }
                catch (Exception e)
                {
                    ApplicationTraceSources.Default.TraceError(e);

                    this.ConnectionStatus = Status.ConnectionFailed;
                    this.ConnectionFailed?.Invoke(
                        this,
                        new ConnectionErrorEventArgs(e));

                    // Notify listeners.
                    await this.eventService.FireAsync(
                        new SessionAbortedEvent(this.Instance, e))
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
                        new SessionEndedEvent(this.Instance))
                        .ConfigureAwait(true);
                }
            }
        }

        public async Task SendAsync(string command)
        {
            if (this.currentConnection != null)
            {
                RecordSentData(command);
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

        //---------------------------------------------------------------------
        // Diagnostics.
        //---------------------------------------------------------------------

        private void RecordReceivedData(string data)
        {
            // Keep buffer if DEBUG or tracing enabled.
#if DEBUG
#else
            if (ApplicationTraceSources.Default.Switch.ShouldTrace(TraceEventType.Verbose))
#endif
            {
                this.receivedData.Append(data);
            }
        }

        private void RecordSentData(string data)
        {
            // Keep buffer if DEBUG or tracing enabled.
#if DEBUG
#else
            if (ApplicationTraceSources.Default.Switch.ShouldTrace(TraceEventType.Verbose))
#endif
            {
                this.sentData.Append(data);
            }
        }

        public void CopySentDataToClipboard()
        {
            if (this.sentData.Length > 0)
            {
                Clipboard.SetText(this.sentData.ToString());
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
                // 
                // Do not invoke any more view callbacks since they can lead to
                // a deadlock: If the connection is being disposed, odds are
                // that the window (ViewInvoker) has already been destructed.
                // That could cause InvokeAndForget to hang, causing a deadlock
                // between the UI thread and the SSH worker thread.
                //

                this.View = null;

                this.currentConnection?.Dispose();
                this.authorizedKey.Dispose();
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

    public class ConnectionErrorEventArgs
    {
        public Exception Error { get; }

        public ConnectionErrorEventArgs(Exception error)
        {
            this.Error = error;
        }
    }

    public class AuthenticationPromptEventArgs
    {
        public bool IsPasswordPrompt { get; }
        public string Prompt { get; }
        public string Response { get; set; }

        public AuthenticationPromptEventArgs(
            string prompt,
            bool isPasswordPrompt)
        {
            this.IsPasswordPrompt = isPasswordPrompt;
            this.Prompt = prompt;
        }
    }

    public class OsLoginAuthenticationFailedException : Exception, IExceptionWithHelpTopic
    {
        public IHelpTopic Help { get; }

        public OsLoginAuthenticationFailedException(
            string message,
            Exception inner,
            IHelpTopic helpTopic)
            : base(message, inner)
        {
            this.Help = helpTopic;
        }
    }
}
