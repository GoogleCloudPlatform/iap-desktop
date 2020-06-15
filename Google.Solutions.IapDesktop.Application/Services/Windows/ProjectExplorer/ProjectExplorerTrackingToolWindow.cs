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
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Util;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer
{
    [ComVisible(false)]
    [SkipCodeCoverage("GUI plumbing")]
    public class ProjectExplorerTrackingToolWindow<TViewModel> : ToolWindow
    {

        private IContainer currentBindingContainer = null;
        private IProjectExplorerNode ignoredNode = null;

        private DockPanel dockPanel;

        private CancellationTokenSource tokenSourceForCurrentTask = null;

        protected ProjectExplorerTrackingToolWindow()
        {
            // Designer only.
        }

        public ProjectExplorerTrackingToolWindow(
            DockPanel dockPanel,
            IProjectExplorer projectExplorer,
            IEventService eventService)
        {
            //
            // This window is a singleton, so we never want it to be closed,
            // just hidden.
            //
            this.HideOnClose = true;
            this.dockPanel = dockPanel;


            // Use currently selected node.
            OnProjectExplorerNodeSelected(projectExplorer.SelectedNode);

            // Track current selection in project explorer.
            eventService.BindHandler<ProjectExplorerNodeSelectedEvent>(
                e => OnProjectExplorerNodeSelected(e.SelectedNode));
        }

        public void ShowWindow()
        {
            this.TabText = this.Text;
            this.ShowOrActivate(
                this.dockPanel, 
                WeifenLuo.WinFormsUI.Docking.DockState.DockBottomAutoHide);
        }

        protected override void OnUserVisibilityChanged(bool visible)
        {
            if (visible && this.ignoredNode != null)
            {
                // There was a selection change while the window was hidden
                // and we were ignoring updates. 
                TraceSources.IapDesktop.TraceVerbose("Reapplying ignored selection change");
                OnProjectExplorerNodeSelected(this.ignoredNode);
            }
        }

        protected void OnProjectExplorerNodeSelected(IProjectExplorerNode node)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(this.IsUserVisible))
            {
                if (!this.IsUserVisible)
                {
                    // The window is currently not visible to the user, so
                    // do not bother updating it immediately. 
                    this.ignoredNode = node;
                    return;
                }
                else
                {
                    this.ignoredNode = null;
                }

                SwitchToNode(node);
            }
        }

        //---------------------------------------------------------------------
        // Overridables.
        //
        // NB. These methods should be abstract, but the forms designer does not like
        // abstract base classes for forms.
        //---------------------------------------------------------------------

        protected virtual void SwitchToNode(
            IProjectExplorerNode node)
        {
        }
    }
}
