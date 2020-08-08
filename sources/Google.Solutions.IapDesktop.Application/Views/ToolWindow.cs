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
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Views

{
    [ComVisible(false)]
    [SkipCodeCoverage("GUI plumbing")]
    public partial class ToolWindow : DockContent
    {
        public ContextMenuStrip TabContextStrip => this.contextMenuStrip;

        public ToolWindow()
        {
            InitializeComponent();
            AutoScaleMode = AutoScaleMode.Dpi;
        }

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

        public void ShowOrActivate(DockPanel dockPanel, DockState defaultState)
        {
            // NB. IsHidden indicates that the window is not shown at all,
            // not even as auto-hide.
            if (this.IsHidden)
            {
                // Show in default position.
                Show(dockPanel, defaultState);
            }

            // If the window is in auto-hide mode, simply activating
            // is not enough.
            switch (this.VisibleState)
            {
                case DockState.DockTopAutoHide:
                case DockState.DockBottomAutoHide:
                case DockState.DockLeftAutoHide:
                case DockState.DockRightAutoHide:
                    dockPanel.ActiveAutoHideContent = this;
                    break;
            }

            // Move focus to window.
            Activate();
        }


        //---------------------------------------------------------------------
        // Track visibility.
        //
        // NB. The DockPanel library does not provide very good events for 
        // tracking whether a tool window is *really* visible or not, so these
        // methods provide a non-perfect approximation.
        //---------------------------------------------------------------------

        protected bool IsUserVisible = false;

        protected override void OnEnter(EventArgs e)
        {
            if (!this.IsUserVisible)
            {
                this.IsUserVisible = true;
                OnUserVisibilityChanged(this.IsUserVisible);
            }
        }

        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);

            switch (this.VisibleState)
            {
                case DockState.DockTopAutoHide:
                case DockState.DockBottomAutoHide:
                case DockState.DockLeftAutoHide:
                case DockState.DockRightAutoHide:
                    if (this.IsUserVisible)
                    {
                        this.IsUserVisible = false;
                        OnUserVisibilityChanged(this.IsUserVisible);
                    }

                    break;
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            if (this.IsUserVisible)
            {
                this.IsUserVisible = false;
                OnUserVisibilityChanged(this.IsUserVisible);
            }
        }

        protected virtual void OnUserVisibilityChanged(bool visible)
        {
        }
    }
}