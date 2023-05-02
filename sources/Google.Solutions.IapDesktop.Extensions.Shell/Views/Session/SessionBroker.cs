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

using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Session;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;
using Google.Solutions.Mvvm.Binding.Commands;
using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Session
{
    public interface IInstanceSessionBroker : ISessionBroker
    {
        /// <summary>
        /// Command menu for sessions, exposed in the main menu
        /// and as context menu.
        /// </summary>
        ICommandContainer<ISession> SessionMenu { get; }

        /// <summary>
        /// Create a new SSH session.
        /// </summary>
        Task<ISession> CreateSessionAsync(
            ISessionContext<SshCredential, SshSessionParameters> context);

        /// <summary>
        /// Create a new RDP session.
        /// </summary>
        Task<ISession> CreateSessionAsync(
            ISessionContext<RdpCredential, RdpSessionParameters> context);
    }

    [Service(typeof(IInstanceSessionBroker), ServiceLifetime.Singleton)]
    [ServiceCategory(typeof(ISessionBroker))]
    public class InstanceSessionBroker : IInstanceSessionBroker
    {
        private const TabAccentColorIndex AccentColorForNonIapSessions = TabAccentColorIndex.Hightlight1;
        private const TabAccentColorIndex AccentColorForUrlBasedSessions = TabAccentColorIndex.Hightlight2;

        private readonly IToolWindowHost toolWindowHost;
        private readonly IMainWindow mainForm;
        private readonly IJobService jobService;

        public InstanceSessionBroker(
            IMainWindow mainForm,
            IToolWindowHost toolWindowHost,
            IJobService jobService)
        {
            this.mainForm = mainForm;
            this.toolWindowHost = toolWindowHost;
            this.jobService = jobService;

            //
            // NB. The ServiceCategory attribute causes this class to be 
            // announced to the global connection broker.
            //

            //
            // Register Session menu.
            //
            // On pop-up of the menu, query the active session and use it as context.
            //
            this.SessionMenu = this.mainForm.AddMenu(
                "&Session", 1,
                () => this.ActiveSession);
        }

        internal InstanceSessionBroker(IServiceProvider serviceProvider)
            : this(
                  serviceProvider.GetService<IMainWindow>(),
                  serviceProvider.GetService<IToolWindowHost>(),
                  serviceProvider.GetService<IJobService>())
        {

        }

        //---------------------------------------------------------------------
        // Session initialization.
        //---------------------------------------------------------------------

        private struct AuthorizationResult<TCredential>
        {
            public TCredential Credential;
            public ITransport Transport;
        }

        private void OnSessionConnected(SessionViewBase session)
        {
            //
            // Add context menu.
            //
            Debug.Assert(session.ContextCommands == null);
            session.ContextCommands = this.SessionMenu;
        }

        private void ApplyTabStyle<TParameters>(
            DockContentHandler dockHandler, 
            Transport.TransportType transportType,
            bool isCreatedFromUrl,
            InstanceLocator instance,
            ISessionCredential credential,
            TParameters sessionParameters)
        {
            //
            // Apply accent color if the session deviates from the norm.
            //
            if (isCreatedFromUrl)
            {
                dockHandler.TabAccentColor = AccentColorForUrlBasedSessions;
            }
            else if (transportType == Transport.TransportType.Vpc)
            {
                dockHandler.TabAccentColor = AccentColorForNonIapSessions;
            }

            var toolTip = new StringBuilder();
            toolTip.AppendLine($"User: {credential}");
            toolTip.AppendLine($"Instance: {instance.Name}");
            toolTip.AppendLine($"Project: {instance.ProjectId}");
            
            if (transportType == Transport.TransportType.IapTunnel)
            {
                toolTip.AppendLine();
                toolTip.AppendLine("Connected through Identity-Aware Proxy.");
            }

#if DEBUG
            toolTip.AppendLine();
            toolTip.AppendLine(sessionParameters.DumpProperties());
#endif

            dockHandler.ToolTipText = toolTip.ToString();
        }

        private Task<AuthorizationResult<TCredential>> CreateTransportAndAuthorizeAsync
            <TCredential, TParameters>(
            ISessionContext<TCredential, TParameters> context)
            where TCredential : ISessionCredential
        {
            return this.jobService.RunInBackground(
                new JobDescription(
                    $"Connecting to {context.Instance.Name}...",
                    JobUserFeedbackType.BackgroundFeedback),
                async cancellationToken =>
                {
                    var credentialTask = context.AuthorizeCredentialAsync(cancellationToken);
                    var transportTask = context.ConnectTransportAsync(cancellationToken);

                    await Task.WhenAll(credentialTask, transportTask)
                        .ConfigureAwait(true);

                    return new AuthorizationResult<TCredential>
                    {
                        Credential = credentialTask.Result,
                        Transport = transportTask.Result
                    };
                });
        }

        internal IRemoteDesktopSession ConnectRdpSession(
            ITransport transport,
            RdpSessionParameters parameters,
            RdpCredential credential)
        {
            Debug.Assert(this.mainForm.IsWindowThread());

            var window = this.toolWindowHost.GetToolWindow<RemoteDesktopView, RemoteDesktopViewModel>();

            window.ViewModel.Instance = transport.Instance;
            window.ViewModel.Server = IPAddress.IsLoopback(transport.Endpoint.Address)
                ? "localhost"
                : transport.Endpoint.Address.ToString();
            window.ViewModel.Port = (ushort)transport.Endpoint.Port;
            window.ViewModel.Parameters = parameters;
            window.ViewModel.Credential = credential;

            var session = window.Bind();

            ApplyTabStyle(
                session.DockHandler,
                parameters.TransportType,
                parameters.Sources.HasFlag(RdpSessionParameters.ParameterSources.Url),
                session.Instance,
                credential,
                parameters);

            window.Show();
            session.Connect();

            OnSessionConnected(session);

            return session;
        }

        internal async Task<ISshTerminalSession> ConnectSshSessionAsync(
            ITransport transport,
            SshSessionParameters parameters,
            SshCredential credential)
        {
            Debug.Assert(this.mainForm.IsWindowThread());

            var window = this.toolWindowHost.GetToolWindow<SshTerminalView, SshTerminalViewModel>();

            window.ViewModel.Instance = transport.Instance;
            window.ViewModel.Endpoint = transport.Endpoint;
            window.ViewModel.AuthorizedKey = credential.Key;
            window.ViewModel.Language = parameters.Language;
            window.ViewModel.ConnectionTimeout = parameters.ConnectionTimeout;

            var session = window.Bind();

            ApplyTabStyle(
                session.DockHandler,
                parameters.TransportType,
                false,
                session.Instance,
                credential,
                parameters);

            window.Show();

            await session.ConnectAsync()
                .ConfigureAwait(false);

            OnSessionConnected(session);

            return session;
        }

        //---------------------------------------------------------------------
        // ISessionBroker.
        //---------------------------------------------------------------------

        public ISession ActiveSession
        {
            get => (ISession)RemoteDesktopView.TryGetActivePane(this.mainForm)
                    ?? SshTerminalView.TryGetActivePane(this.mainForm)
                    ?? null;
        }

        public bool IsConnected(InstanceLocator vmInstance)
        {
            return
                RemoteDesktopView.TryGetExistingPane(this.mainForm, vmInstance) != null ||
                SshTerminalView.TryGetExistingPane(this.mainForm, vmInstance) != null;
        }

        public bool TryActivate(InstanceLocator vmInstance, out ISession session)
        {
            if (RemoteDesktopView.TryGetExistingPane(this.mainForm, vmInstance) is
                RemoteDesktopView existingRdpSession &&
                existingRdpSession != null)
            {
                // Pane found, activate.
                existingRdpSession.SwitchToDocument();
                session = existingRdpSession;
                return true;
            }
            else if (SshTerminalView.TryGetExistingPane(this.mainForm, vmInstance) is
                SshTerminalView existingSshSession &&
                existingSshSession != null)
            {
                // Pane found, activate.
                existingSshSession.SwitchToDocument();
                session = existingSshSession;
                return true;
            }
            else
            {
                session = null;
                return false;
            }
        }

        //---------------------------------------------------------------------
        // IInstanceSessionBroker.
        //---------------------------------------------------------------------

        public ICommandContainer<ISession> SessionMenu { get; }

        public async Task<ISession> CreateSessionAsync(
            ISessionContext<SshCredential, SshSessionParameters> context)
        {
            var result = await CreateTransportAndAuthorizeAsync(context)
                .ConfigureAwait(true);

            //
            // Back on the UI thread, create the corresponding view.
            //

            var session = await ConnectSshSessionAsync(
                    result.Transport,
                    context.Parameters,
                    result.Credential)
                .ConfigureAwait(true);

            ((SessionViewBase)session).Disposed += (_, __) => context.Dispose();

            return (ISession)session;
        }

        public async Task<ISession> CreateSessionAsync(
            ISessionContext<RdpCredential, RdpSessionParameters> context)
        {
            var result = await CreateTransportAndAuthorizeAsync(context)
                .ConfigureAwait(true);

            //
            // Back on the UI thread, create the corresponding view.
            //

            var session = ConnectRdpSession(
                result.Transport,
                context.Parameters,
                result.Credential);

            ((SessionViewBase)session).Disposed += (_, __) => context.Dispose();

            return (ISession)session;
        }
    }
}
