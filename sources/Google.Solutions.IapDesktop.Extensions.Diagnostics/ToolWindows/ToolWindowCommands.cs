//
// Copyright 2023 Google LLC
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

using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Diagnostics.ToolWindows
{
    [MenuCommand(typeof(DebugMenu), Rank = 0x100)]
    [Service]
    public class OpenJobServiceToolWindow
        : OpenToolWindowCommand<DebugMenu.Context, DebugJobServiceView, DebugJobServiceViewModel>
    {
        public OpenJobServiceToolWindow(IToolWindowHost toolWindowHost)
            : base(
                  toolWindowHost,
                  "&Job Service",
                  _ => true,
                  _ => true)
        {
        }
    }

    [MenuCommand(typeof(DebugMenu), Rank = 0x101)]
    [Service]
    public class OpenDockingToolWindow
        : OpenToolWindowCommand<DebugMenu.Context, DebugDockingView, DebugDockingViewModel>
    {
        public OpenDockingToolWindow(IToolWindowHost toolWindowHost)
            : base(
                  toolWindowHost,
                  "&Docking",
                  _ => true,
                  _ => true)
        {
        }
    }

    [MenuCommand(typeof(DebugMenu), Rank = 0x102)]
    [Service]
    public class OpenProjectExplorerTrackingToolWindow
        : OpenToolWindowCommand<DebugMenu.Context, DebugProjectExplorerTrackingView, DebugProjectExplorerTrackingViewModel>
    {
        public OpenProjectExplorerTrackingToolWindow(IToolWindowHost toolWindowHost)
            : base(
                  toolWindowHost,
                  "&Project Explorer Tracking",
                  _ => true,
                  _ => true)
        {
        }
    }

    [MenuCommand(typeof(DebugMenu), Rank = 0x104)]
    [Service]
    public class OpenFullScreenPaneToolWindow
        : OpenToolWindowCommand<DebugMenu.Context, DebugFullScreenView, DebugFullScreenViewModel>
    {
        public OpenFullScreenPaneToolWindow(IToolWindowHost toolWindowHost)
            : base(
                  toolWindowHost,
                  "&Full screen pane",
                  _ => true,
                  _ => true)
        {
        }
    }

    [MenuCommand(typeof(DebugMenu), Rank = 0x105)]
    [Service]
    public class OpenThemeToolWindow
        : OpenToolWindowCommand<DebugMenu.Context, DebugThemeView, DebugThemeViewModel>
    {
        public OpenThemeToolWindow(IToolWindowHost toolWindowHost)
            : base(
                  toolWindowHost,
                  "&Theme",
                  _ => true,
                  _ => true)
        {
        }
    }

    [MenuCommand(typeof(DebugMenu), Rank = 0x106)]
    [Service]
    public class OpenRegisteredServicesToolWindow
        : OpenToolWindowCommand<DebugMenu.Context, DebugServiceRegistryView, DebugServiceRegistryViewModel>
    {
        public OpenRegisteredServicesToolWindow(IToolWindowHost toolWindowHost)
            : base(
                  toolWindowHost,
                  "&Registered services",
                  _ => true,
                  _ => true)
        {
        }
    }

    [MenuCommand(typeof(DebugMenu), Rank = 0x107)]
    [Service]
    public class OpenCommonControlsToolWindow
        : OpenToolWindowCommand<DebugMenu.Context, DebugCommonControlsView, DebugCommonControlsViewModel>
    {
        public OpenCommonControlsToolWindow(IToolWindowHost toolWindowHost)
            : base(
                  toolWindowHost,
                  "&Common controls",
                  _ => true,
                  _ => true)
        {
        }
    }

    [MenuCommand(typeof(DebugMenu), Rank = 0x108)]
    [Service]
    public class OpenAndCloseLotsOfToolWindows : MenuCommandBase<DebugMenu.Context>
    {
        private readonly IToolWindowHost toolWindowHost;

        public OpenAndCloseLotsOfToolWindows(IToolWindowHost toolWindowHost)
            : base("Open and &close lots of windows")
        {
            this.toolWindowHost = toolWindowHost;
        }

        public override async Task ExecuteAsync(DebugMenu.Context context)
        {
            Debug.Assert(IsAvailable(context) && IsEnabled(context));

            //
            // Cause unreasonable stress on the windowing system
            // by opening and closing a lot of document windows.
            //
            for (int i = 0; i < 100; i++)
            {
                this.toolWindowHost
                    .GetToolWindow<AutoCloseView, AutoCloseViewModel>()
                    .Show();

                await Task.Delay(100);
            }
        }

        protected override bool IsAvailable(DebugMenu.Context context)
        {
            return true;
        }

        protected override bool IsEnabled(DebugMenu.Context context)
        {
            return true;
        }
    }
}
