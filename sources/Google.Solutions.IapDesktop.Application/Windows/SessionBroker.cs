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

using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Linq;
using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Binding.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Windows
{
    /// <summary>
    /// An active session to a VM.
    /// </summary>
    public interface ISession : IDisposable
    {
        /// <summary>
        /// Disconnect and close session.
        /// </summary>
        void Close();

        /// <summary>
        /// Check if session is connected (and not dead).
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Check if the session supports downloading and uploading
        /// files.
        /// </summary>
        bool CanTransferFiles { get; }

        /// <summary>
        /// Transfer files from or to the remote VM.
        /// </summary>
        Task TransferFilesAsync();

        /// <summary>
        /// Instance this session is connected to.
        /// </summary>
        InstanceLocator Instance { get; }

        /// <summary>
        /// Check if the session is in the process of closing.
        /// </summary>
        bool IsClosing { get; }

        /// <summary>
        /// Switch focus to this session.
        /// </summary>
        void ActivateSession();
    }

    /// <summary>
    /// Broker that keeps track of active sessions.
    /// </summary>
    public interface ISessionBroker
    {
        /// <summary>
        /// Command menu for sessions, exposed in the main menu
        /// and as context menu.
        /// </summary>
        ICommandContainer<ISession> SessionMenu { get; }

        /// <summary>
        /// Return active session, or null if no session is active.
        /// </summary>
        ISession? ActiveSession { get; }

        /// <summary>
        /// Check if there is a session for a VM instance.
        /// </summary>
        bool IsConnected(InstanceLocator vmInstance);

        /// <summary>
        /// Activate session to VM instance, if any.
        /// </summary>
        bool TryActivateSession(
            InstanceLocator vmInstance,
            out ISession? session);

        /// <summary>
        /// Return a list of all sessions.
        /// </summary>
        IReadOnlyCollection<ISession> Sessions { get; }
    }

    public class SessionBroker : ISessionBroker
    {
        private readonly IMainWindow mainForm;

        public SessionBroker(IMainWindow mainForm)
        {
            this.mainForm = mainForm.ExpectNotNull(nameof(mainForm));

            //
            // Register Session menu.
            //
            // On pop-up of the menu, query the active session and use it as context.
            //
            this.SessionMenu = this.mainForm.AddMenu(
                "&Session", 1,
                () => this.ActiveSession);
        }

        //---------------------------------------------------------------------
        // ISessionBroker.
        //---------------------------------------------------------------------

        public ICommandContainer<ISession> SessionMenu { get; }

        public bool IsConnected(InstanceLocator vmInstance)
        {
            return this.mainForm.MainPanel
                .Documents
                .EnsureNotNull()
                .OfType<ISession>()
                .Where(pane => pane.Instance == vmInstance && !pane.IsClosing)
                .Any();
        }

        public ISession? ActiveSession
        {
            //
            // NB. The active content might be in a float window.
            //
            get => this.mainForm.MainPanel.ActivePane?.ActiveContent as ISession;
        }

        public bool TryActivateSession(
            InstanceLocator vmInstance,
            out ISession? session)
        {
            var existingSession = this.mainForm.MainPanel
                .Documents
                .EnsureNotNull()
                .OfType<ISession>()
                .Where(pane => pane.Instance == vmInstance && !pane.IsClosing)
                .FirstOrDefault();
            if (existingSession != null)
            {
                //
                // Session found, activate.
                //
                existingSession.ActivateSession();
                session = existingSession;
                return true;
            }
            else
            {
                session = null;
                return false;
            }
        }

        public IReadOnlyCollection<ISession> Sessions
        {
            get
            {
                var floatWindowDocs = this.mainForm.MainPanel
                    .FloatWindows
                    .EnsureNotNull()
                    .SelectMany(fw => fw.DockPanel.Documents.EnsureNotNull());

                var mainWindowDocs = this.mainForm.MainPanel
                    .Documents
                    .EnsureNotNull();

                return mainWindowDocs
                    .Concat(floatWindowDocs)
                    .OfType<ISession>()
                    .Where(pane => !pane.IsClosing)
                    .ToList();
            }
        }
    }

    //-------------------------------------------------------------------------
    // Events.
    //-------------------------------------------------------------------------

    public abstract class SessionBrokerEventBase
    {
        public InstanceLocator Instance { get; }

        protected SessionBrokerEventBase(InstanceLocator vmInstance)
        {
            this.Instance = vmInstance;
        }
    }

    public class SessionStartedEvent : SessionBrokerEventBase
    {
        public SessionStartedEvent(InstanceLocator vmInstance) : base(vmInstance)
        {
        }
    }

    public class SessionAbortedEvent : SessionBrokerEventBase
    {
        public Exception Exception { get; }

        public SessionAbortedEvent(InstanceLocator vmInstance, Exception exception)
            : base(vmInstance)
        {
            this.Exception = exception;
        }
    }

    public class SessionEndedEvent : SessionBrokerEventBase
    {
        public Exception? Exception { get; }

        public SessionEndedEvent(InstanceLocator vmInstance) : base(vmInstance)
        {
        }
    }
}
