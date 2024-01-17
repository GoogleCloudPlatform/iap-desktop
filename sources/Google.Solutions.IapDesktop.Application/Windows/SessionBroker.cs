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
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding.Commands;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Windows
{
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
        /// Download a file from the remote VM.
        /// </summary>
        Task DownloadFilesAsync();
        
        /// <summary>
        /// Upload a file to the remote VM.
        /// </summary>
        Task UploadFilesAsync();

        /// <summary>
        /// Instance this session is connected to.
        /// </summary>
        InstanceLocator Instance { get; }

        bool IsFormClosing { get; } //TODO: Rename to IsClosing.
        void SwitchToDocument(); //TODO: Rename to Activate
    }

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
        ISession ActiveSession { get; }

        /// <summary>
        /// Check if there is an active session for a VM instance.
        /// </summary>
        bool IsConnected(InstanceLocator vmInstance);

        /// <summary>
        /// Activate session to VM instance, if any.
        /// </summary>
        bool TryActivate(
            InstanceLocator vmInstance,
            out ISession session);
    }

    public class GlobalSessionBroker : ISessionBroker // TODO: Rename, split file
    {
        private readonly IMainWindow mainForm;

        public GlobalSessionBroker(IMainWindow mainForm)
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
                .Where(pane => pane.Instance == vmInstance && !pane.IsFormClosing)
                .Any();
        }

        public ISession ActiveSession
        {
            //
            // NB. The active content might be in a float window.
            //
            get => this.mainForm.MainPanel.ActivePane?.ActiveContent as ISession;
        }

        public bool TryActivate(
            InstanceLocator vmInstance,
            out ISession session)
        {
            var existingSession = this.mainForm.MainPanel
                .Documents
                .EnsureNotNull()
                .OfType<ISession>()
                .Where(pane => pane.Instance == vmInstance && !pane.IsFormClosing)
                .FirstOrDefault();
            if (existingSession != null)
            {
                //
                // Session found, activate.
                //
                existingSession.SwitchToDocument();
                session = existingSession;
                return true;
            }
            else
            {
                session = null;
                return false;
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
        public Exception Exception { get; }

        public SessionEndedEvent(InstanceLocator vmInstance) : base(vmInstance)
        {
        }
    }
}
