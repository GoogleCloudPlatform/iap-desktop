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

using Google.Solutions.IapDesktop.Application.Controls;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Windows;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Activity.Events;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services.EventLog
{
    [Service(ServiceLifetime.Singleton)]
    internal partial class EventLogWindow 
        : ProjectExplorerTrackingToolWindow<ActivityLogViewModel>
    {
        private const int CacheCapacity = 5;

        public EventLogWindow(IServiceProvider serviceProvider)
            : base(
                  serviceProvider.GetService<IMainForm>().MainPanel,
                  serviceProvider.GetService<IProjectExplorer>(),
                  serviceProvider.GetService<IEventService>(),
                  CacheCapacity
                  )
        {
            InitializeComponent();

            this.theme.ApplyTo(this.toolStrip);
        }

        //---------------------------------------------------------------------
        // ProjectExplorerTrackingToolWindow.
        //---------------------------------------------------------------------

        protected override async Task<ActivityLogViewModel> LoadViewModelAsync(
            IProjectExplorerNode node,
            CancellationToken token)
        {
            if (node is IProjectExplorerVmInstanceNode vmNode)
            {
                await Task.Delay(2000, token).ConfigureAwait(false);
                return new ActivityLogViewModel(vmNode);
            }
            else
            {
                // We do not care about any other nodes.
                return null;
            }
        }

        protected override void BindViewModel(
            ActivityLogViewModel model, 
            IContainer bindingContainer)
        {
            //this.label1.BindProperty(
            //    c => c.Text,
            //    model,
            //    m => m.InstanceName,
            //    bindingContainer);
        }
    }

    public class EventsListView : BindableListView<EventBase>
    { }
}
