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
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Windows;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Util;
using System;
using System.ComponentModel;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services.ActivityLog
{
    public class ProjectExplorerTrackingToolWindow<TViewModel> : ToolWindow
    {
        // Keep a cache of view models for the last N project explorer nodes
        // visited so that switching back and forth between nodes is faster.
        private readonly LeastRecentlyUsedCache<IProjectExplorerNode, TViewModel> modelCache;

        private IContainer currentBindingContainer = null;

        protected ProjectExplorerTrackingToolWindow()
        {
            // Designer only.
        }

        public ProjectExplorerTrackingToolWindow(
            IProjectExplorer projectExplorer,
            IEventService eventService,
            int cacheCapacity)
        {
            this.modelCache = new LeastRecentlyUsedCache<IProjectExplorerNode, TViewModel>(
                cacheCapacity);

            // Use currently selected node.
            OnProjectExplorerNodeSelected(projectExplorer.SelectedNode);

            // Track current selection in project explorer.
            eventService.BindHandler<ProjectExplorerNodeSelectedEvent>(
                e => OnProjectExplorerNodeSelected(e.SelectedNode));
        }

        public void ShowWindow(DockPanel dockPanel)
        {
            this.ShowOrActivate(
                dockPanel, 
                WeifenLuo.WinFormsUI.Docking.DockState.DockBottomAutoHide);
        }

        protected virtual TViewModel LoadViewModel(IProjectExplorerNode node)
        {
            // This method should be abstract, but the forms designer does not like
            // abstract base classes for forms.
            throw new InvalidOperationException();
        }

        protected virtual void BindViewModel(
            TViewModel model, 
            IContainer bindingContainer)
        {
        }

        protected void OnProjectExplorerNodeSelected(IProjectExplorerNode node)
        {
            TViewModel model = this.modelCache.Lookup(node);
            if (model == null)
            {
                TraceSources.IapDesktop.TraceVerbose(
                    "Loading view model ({0})", typeof(TViewModel).Name);

                model = LoadViewModel(node);
            }

            if (model != null)
            { 
                this.modelCache.Add(node, model);

                TraceSources.IapDesktop.TraceVerbose(
                    "Binding view model ({0})", typeof(TViewModel).Name);

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
    }
}
