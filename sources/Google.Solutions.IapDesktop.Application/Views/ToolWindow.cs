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
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Views
{
    [ComVisible(false)]
    [SkipCodeCoverage("GUI plumbing")]
    public partial class ToolWindow : DockContent
    {
        private readonly DockPanel panel;
        private readonly DockState initialDockState;
        private DockState lastDockState;

        public ContextMenuStrip TabContextStrip => this.contextMenuStrip;

        public bool IsClosed { get; private set; } = false;

        public ToolWindow()
        {
            InitializeComponent();
            AutoScaleMode = AutoScaleMode.Dpi;
        }

        public ToolWindow(
            IServiceProvider serviceProvider,
            DockState defaultDockState) : this()
        {
            this.panel = serviceProvider.GetService<IMainForm>().MainPanel;
            var stateRepository = serviceProvider.GetService<ToolWindowStateRepository>();

            this.lastDockState = defaultDockState;

            // Read persisted window state.
            var state = stateRepository.GetSetting(
                GetType().Name, // Unique name of tool window
                defaultDockState);
            this.initialDockState = state.DockState.EnumValue;

            // Save persisted window state.
            this.Disposed += (sender, args) =>
            {
                //
                // NB. At this point, it's too late to read this.DockState,
                // so we have to rely on the value captured during previous
                // state transitions.
                //

                try
                {
                    if (
                        // If the window was closed, reset its saved state.
                        // Note that we're only interested in storing the
                        // location, not whether the window is visible or not.
                        this.IsClosed ||
                        lastDockState == DockState.Hidden ||
                        lastDockState == DockState.Unknown ||

                        // Ignore Document and Float as these are more complicated
                        // and not worth the trouble.
                        lastDockState == DockState.Document ||
                        lastDockState == DockState.Float)
                    {
                        // Ignore Hidden state as we only want to restore
                        // the dock location, not whether the window is
                        // shown or not. 
                        state.DockState.Reset();
                    }
                    else
                    {
                        // Restore dock state on next run.
                        state.DockState.EnumValue = lastDockState;
                    }

                    stateRepository.SetSetting(state);
                }
                catch (Exception e)
                {
                    ApplicationTraceSources.Default.TraceWarning(
                        "Saving tool window state failed: {0}", e.Message);
                }
            };
        }

        //---------------------------------------------------------------------
        // Show/Hide.
        //---------------------------------------------------------------------

        protected void CloseSafely()
        {
            if (this.HideOnClose)
            {
                Hide();
            }
            else
            {
                Close();
            }
        }

        public virtual void ShowWindow(bool activate)
        {
            Debug.Assert(this.panel != null);

            this.TabText = this.Text;

            // NB. IsHidden indicates that the window is not shown at all,
            // not even as auto-hide.
            if (this.IsHidden)
            {
                // Show in default position.
                Show(this.panel, this.initialDockState);
            }

            if (activate)
            {
                // If the window is in auto-hide mode, simply activating
                // is not enough.
                switch (this.VisibleState)
                {
                    case DockState.DockTopAutoHide:
                    case DockState.DockBottomAutoHide:
                    case DockState.DockLeftAutoHide:
                    case DockState.DockRightAutoHide:
                        this.panel.ActiveAutoHideContent = this;
                        break;
                }

                // Move focus to window.
                Activate();

                //
                // If an auto-hide window loses focus and closes, we fail to 
                // catch that event. 
                // To force an update, disregard the cached state and re-raise
                // the UserVisibilityChanged event.
                //
                OnUserVisibilityChanged(true);
                this.wasUserVisible = true;
            }
        }

        public virtual void ShowWindow() => ShowWindow(true);

        protected bool IsAutoHide
        {
            get
            {
                switch (this.VisibleState)
                {
                    case DockState.DockTopAutoHide:
                    case DockState.DockBottomAutoHide:
                    case DockState.DockLeftAutoHide:
                    case DockState.DockRightAutoHide:
                        return true;

                    default:
                        return false;
                }
            }
        }

        protected bool IsDocked
        {
            get
            {
                switch (this.VisibleState)
                {
                    case DockState.DockTop:
                    case DockState.DockBottom:
                    case DockState.DockLeft:
                    case DockState.DockRight:
                        return true;

                    default:
                        return false;
                }
            }
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void closeMenuItem_Click(object sender, System.EventArgs e)
        {
            this.CloseSafely();
        }

        private void ToolWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Shift && e.KeyCode == Keys.Escape)
            {
                CloseSafely();
            }
        }

        //---------------------------------------------------------------------
        // Track visibility.
        //
        // NB. The DockPanel library does not provide good properties or evens 
        // that would allow you to determine whether a window is effectively
        // visible to the user or not.
        //
        // This table shows the value of key properties based on the window state:
        //
        // 
        // ---------------------------------------------------------------------------------------
        //           |                     |             |         |                   | Pane.ActiveContent
        //           | State               | IsActivated | IsFloat | Visible/DockState | == this
        // ---------------------------------------------------------------------------------------
        // Float     | Single pane         | (any)       | TRUE    | Float             | TRUE    
        //           | Split pane, focus   | FALSE       | TRUE    | Float             | TRUE    
        //           | Split pane, no focus| TRUE        | TRUE    | Float             | TRUE    
        //           | Background          | FALSE       | TRUE    | Float             | FALSE
        // ---------------------------------------------------------------------------------------
        // AutoHide  | Single              | (any)       | FALSE   | DockRightAutoHide | TRUE    
        //           | Background          | (any)       | FALSE   | DockRightAutoHide | TRUE
        // ---------------------------------------------------------------------------------------
        // Dock      | Single pane         | TRUE        | FALSE   | DockRight         | TRUE    
        //           | Split pane, focus   | FALSE (!)   | FALSE   | DockRight         | TRUE    
        //           | Split pane, no focus| TRUE  (!)   | FALSE   | DockRight         | TRUE    
        //           | Background          | FALSE       | FALSE   | DockRight         | FALSE
        // -----------------------------------------------------------------------------------------
        //
        // IsHidden is TRUE during construction, and FALSE ever after.
        // When docked and hidden, the size is reset to (0, 0)
        //

        protected bool IsInBackground =>
            (this.IsFloat && this.Pane.ActiveContent != this) ||
            (this.IsAutoHide && this.Size.Height == 0 && this.Size.Width == 0) ||
            (this.IsDocked && this.Pane.ActiveContent != this);

        protected bool IsUserVisible => !this.IsHidden && !IsInBackground;
        private bool wasUserVisible = false;

        private void RaiseUserVisibilityChanged()
        {
            // Only call OnUserVisibilityChanged if there really was a change.
            if (this.IsUserVisible != this.wasUserVisible)
            {
                OnUserVisibilityChanged(this.IsUserVisible);
                this.wasUserVisible = this.IsUserVisible;
            }
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);

            this.lastDockState = this.DockState;
            RaiseUserVisibilityChanged();
        }

        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);

            this.lastDockState = this.DockState;
            RaiseUserVisibilityChanged();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            this.lastDockState = this.DockState;
            RaiseUserVisibilityChanged();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            this.IsClosed = false;

            this.lastDockState = this.DockState;
            RaiseUserVisibilityChanged();
        }

        protected override void OnClosed(EventArgs e)
        {
            // NB. This method might be invoked more than once if a disconnect
            // event coincides (which is reasnably common when closing the app
            // with active sessions).

            base.OnClosed(e);

            this.IsClosed = true;
        }

        protected virtual void OnUserVisibilityChanged(bool visible)
        {
            // Can be overriden in derived class.
        }
    }
}