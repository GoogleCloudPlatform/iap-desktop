//
// Copyright 2024 Google LLC
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
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Theme;
using Google.Solutions.Terminal.Controls;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session
{
    public abstract class ClientViewBase<TClient> : SessionViewBase
        where TClient : ClientBase, new()
    {
        private readonly IExceptionDialog exceptionDialog;
        private readonly IEventQueue eventQueue;
        private readonly IControlTheme theme;

        protected TClient? Client { get; private set; }

        public bool IsClosing { get; private set; } = false;

        protected ClientViewBase(
            IMainWindow mainWindow,
            ToolWindowStateRepository stateRepository,
            IEventQueue eventQueue,
            IExceptionDialog exceptionDialog,
            IControlTheme theme,
            IBindingContext bindingContext)
            : base(mainWindow, stateRepository, bindingContext)
        {
            this.exceptionDialog = exceptionDialog;
            this.eventQueue = eventQueue;
            this.theme = theme;
        }

        public void Connect()
        {
            Precondition.Expect(this.Client == null, "View has been connected before");

            //
            // Do initialization here (as opposed to in the constructor)
            // to ensure that we have a window handle.
            //
            SuspendLayout();

            this.Client = new TClient()
            {
                Size = this.Size,
                Dock = DockStyle.Fill
            };
            this.Controls.Add(this.Client);

            this.Client.ConnectionClosed += OnClientConnectionClosed;
            this.Client.ConnectionFailed += OnClientConnectionFailed;
            this.Client.StateChanged += OnClientStateChanged;

            //
            // Because we're not initializing controls in the constructor, the
            // theme isn't applied by default.
            //
            Debug.Assert(this.theme != null || Install.IsExecutingTests);

            this.theme?.ApplyTo(this);

            ResumeLayout(false);

            ConnectCore();
        }

        public bool IsConnected
        {
            get =>
                this.Client != null && (
                this.Client.State == ConnectionState.Connected ||
                this.Client.State == ConnectionState.LoggedOn);
        }

        //---------------------------------------------------------------------
        // State tracking event handlers.
        //---------------------------------------------------------------------

        private void OnClientStateChanged(object sender, System.EventArgs e)
        {
            if (this.Client!.State == ConnectionState.Connected)
            {
                _ = this.eventQueue.PublishAsync(new SessionStartedEvent(this.Instance));
            }
        }

        private void OnClientConnectionFailed(object sender, Mvvm.Controls.ExceptionEventArgs e)
        {
            OnFatalError(e.Exception);

            _ = this.eventQueue.PublishAsync(new SessionAbortedEvent(this.Instance, e.Exception));
        }

        private void OnClientConnectionClosed(object sender, ClientBase.ConnectionClosedEventArgs e)
        {
            switch (e.Reason)
            {
                case ClientBase.DisconnectReason.ReconnectInitiatedByUser:
                    //
                    // User initiated a reconnect -- leave everything as is.
                    //
                    break;

                case RdpClient.DisconnectReason.FormClosed:
                    //
                    // User closed the form.
                    //
                    break;

                case RdpClient.DisconnectReason.DisconnectedByUser:
                    //
                    // User-initiated signout.
                    //
                    Close();
                    break;

                default:
                    //
                    // Something else - allow user to reconnect.
                    //
                    break;
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            if (this.Client != null)
            {
                //
                // NB. Docking does not work reliably with the OCX, so keep the size
                // in sync programmatically.
                //
                this.Client.Size = this.Size;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            //
            // Mark this pane as being in closing state even though it is still
            // visible at this point. The flag ensures that this pane is
            // not considered by TryGetExistingPane anymore.
            //
            this.IsClosing = true;

            _ = this.eventQueue.PublishAsync(new SessionEndedEvent(this.Instance));
        }

        //---------------------------------------------------------------------
        // Drag/docking.
        //
        // The client control must always have a parent. But when a document is
        // dragged to become a floating window, or when a window is re-docked,
        // then its parent is temporarily set to null.
        // 
        // To "rescue" the client control in these situations, we temporarily
        // move the the control to a rescue form when the drag begins, and
        // restore it when it ends.
        //---------------------------------------------------------------------

        private Form? rescueWindow = null;

        protected override Size DefaultFloatWindowClientSize => this.Size;

        protected override void OnDockBegin()
        {
            //
            // NB. It's possible that another rescue operation is still in
            // progress. So don't create a window if there is one already.
            //
            if (this.rescueWindow == null && this.Client != null)
            {
                this.rescueWindow = new Form();
                this.Client.Parent = this.rescueWindow;
            }

            base.OnDockBegin();
        }

        protected override void OnDockEnd()
        {
            if (this.rescueWindow != null && this.Client != null)
            {
                this.Client.Parent = this;
                this.Client.Size = this.Size;
                this.rescueWindow.Close();
                this.rescueWindow = null;
            }

            base.OnDockEnd();
        }

        //---------------------------------------------------------------------
        // Abstract and virtual methods for deriving class to override.
        //---------------------------------------------------------------------

        /// <summary>
        /// Initialize the client and connect.
        /// </summary>
        protected abstract void ConnectCore();

        /// <summary>
        /// Get instance that this session connects to.
        /// </summary>
        public abstract InstanceLocator Instance { get; }

        /// <summary>
        /// Display a fatal error.
        /// </summary>
        protected virtual void OnFatalError(Exception e)
        {
            if (e.IsCancellation())
            {
                //
                // The user cancelled, nervemind.
                //
                return;
            }

            this.exceptionDialog.Show(this, "Session disconnected", e);
        }
    }
}
