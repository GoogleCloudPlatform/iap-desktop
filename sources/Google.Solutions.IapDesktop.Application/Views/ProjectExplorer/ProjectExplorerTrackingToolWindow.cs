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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Views.ProjectExplorer
{
    [ComVisible(false)]
    [SkipCodeCoverage("GUI plumbing")]
    public class ProjectExplorerTrackingToolWindow<TViewModel> : ToolWindow
    {
        private readonly IExceptionDialog exceptionDialog;
        private readonly TaskScheduler taskScheduler;

        private IProjectExplorerNode ignoredNode = null;

        private DockPanel dockPanel;

        protected ProjectExplorerTrackingToolWindow()
        {
            // Designer only.
        }

        public ProjectExplorerTrackingToolWindow(
            DockPanel dockPanel,
            IProjectExplorer projectExplorer,
            IEventService eventService,
            IExceptionDialog exceptionDialog)
        {
            this.exceptionDialog = exceptionDialog;

            // Capture the GUI thread scheduler.
            this.taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

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
            this.ShowOrActivate(this.dockPanel, this.DefaultState);
        }

        protected virtual WeifenLuo.WinFormsUI.Docking.DockState DefaultState
            => WeifenLuo.WinFormsUI.Docking.DockState.DockBottomAutoHide;

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
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(node, this.IsUserVisible))
            {
                if (!this.IsUserVisible)
                {
                    // The window is currently not visible to the user, so
                    // do not bother updating it immediately. 
                    this.ignoredNode = node;

                    TraceSources.IapDesktop.TraceVerbose(
                        "Ignoring switch to {0} because window is not visible", node);

                    return;
                }
                else
                {
                    this.ignoredNode = null;
                }

                SwitchToNodeAsync(node)
                    .ContinueWith(t =>
                    {
                        try
                        {
                            t.Wait();
                        }
                        catch (Exception e)
                        {
                            this.exceptionDialog.Show(this, "Loading data failed", e);
                        }
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.None,

                    // Continue on UI thread. 
                    // Note that there's a bug in the CLR that can cause
                    // TaskScheduler.FromCurrentSynchronizationContext() to become null.
                    // Therefore, use a task scheduler object captured previously.
                    // Cf. https://stackoverflow.com/questions/4659257/
                    this.taskScheduler);
            }
        }

        //---------------------------------------------------------------------
        // Overridables.
        //
        // NB. These methods should be abstract, but the forms designer does not like
        // abstract base classes for forms.
        //---------------------------------------------------------------------

        protected virtual Task SwitchToNodeAsync(IProjectExplorerNode node)
        {
            throw new NotImplementedException();
        }
    }
}
