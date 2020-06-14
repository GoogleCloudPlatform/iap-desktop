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
        // Keep a cache of view models for the last N project explorer nodes
        // visited so that switching back and forth between nodes is faster.
        private readonly LeastRecentlyUsedCache<IProjectExplorerNode, TViewModel> modelCache;

        private readonly TaskScheduler taskScheduler;

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
            IEventService eventService,
            int cacheCapacity)
        {
            // Capture the GUI thread scheduler.
            this.taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

            //
            // This window is a singleton, so we never want it to be closed,
            // just hidden.
            //
            this.HideOnClose = true;
            this.dockPanel = dockPanel;

            this.modelCache = new LeastRecentlyUsedCache<IProjectExplorerNode, TViewModel>(
                cacheCapacity);

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

                TViewModel model = this.modelCache.Lookup(node);
                if (model != null)
                {
                    // Apply model synchronously.
                    this.modelCache.Add(node, model);
                    ApplyViewModel(model);
                }
                else
                {
                    if (this.tokenSourceForCurrentTask != null)
                    {
                        // Another asynchnous load/bind operation is ongoing.
                        // Cancel that one because we won't need its result.

                        TraceSources.IapDesktop.TraceVerbose("Cancelling previous model load task");
                        this.tokenSourceForCurrentTask.Cancel();
                        this.tokenSourceForCurrentTask = null;
                    }

                    // TODO: post job
                    // TODO: allow job to be canelled

                    // Load model.
                    this.tokenSourceForCurrentTask = new CancellationTokenSource();
                    LoadViewModelAsync(node, this.tokenSourceForCurrentTask.Token)
                        .ContinueWith(t =>
                        {
                            try
                            {
                                if (t.Result != null)
                                {
                                    this.modelCache.Add(node, t.Result);
                                    ApplyViewModel(t.Result);
                                }
                            }
                            catch (Exception e) when (e.IsCancellation())
                            {
                                TraceSources.IapDesktop.TraceVerbose("Model load cancelled");
                            }
                        },
                        this.tokenSourceForCurrentTask.Token,
                        TaskContinuationOptions.None,

                        // Continue on UI thread. 
                        // Note that there's a bug in the CLR that can cause
                        // TaskScheduler.FromCurrentSynchronizationContext() to become null.
                        // Therefore, use a task scheduler object captured previously.
                        // Cf. https://stackoverflow.com/questions/4659257/
                        this.taskScheduler);
                }
            }
        }

        private void ApplyViewModel(TViewModel model)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(typeof(TViewModel).Name))
            {
                var bindingContainer = new Container();
                BindViewModel(model, bindingContainer);

                if (this.currentBindingContainer != null)
                {
                    // Dispose all old bindings.
                    this.currentBindingContainer.Dispose();
                }

                this.currentBindingContainer = bindingContainer;
            }
        }

        //---------------------------------------------------------------------
        // Overridables.
        //
        // NB. These methods should be abstract, but the forms designer does not like
        // abstract base classes for forms.
        //---------------------------------------------------------------------

        protected virtual Task<TViewModel> LoadViewModelAsync(
            IProjectExplorerNode node,
            CancellationToken token)
        {
            throw new InvalidOperationException();
        }

        protected virtual void BindViewModel(
            TViewModel model,
            IContainer bindingContainer)
        {}
    }
}
