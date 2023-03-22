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

using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Connection;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop
{
    public interface IRemoteDesktopSession : ISession
    {
        bool TrySetFullscreen(FullScreenMode mode);

        void ShowSecurityScreen();

        void ShowTaskManager();

        bool CanEnterFullScreen { get; }
    }

    public enum FullScreenMode
    {
        Off,
        SingleScreen,
        AllScreens
    }

    public interface IRemoteDesktopSessionBroker : ISessionBroker
    {
        IRemoteDesktopSession ActiveRemoteDesktopSession { get; }

        IRemoteDesktopSession Connect(RdpConnectionTemplate template);
    }

    [Service(typeof(IRemoteDesktopSessionBroker), ServiceLifetime.Singleton, ServiceVisibility.Global)]
    [ServiceCategory(typeof(ISessionBroker))]
    public class RemoteDesktopSessionBroker : IRemoteDesktopSessionBroker
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IMainWindow mainForm;

        public RemoteDesktopSessionBroker(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.mainForm = serviceProvider.GetService<IMainWindow>();

            // NB. The ServiceCategory attribute causes this class to be 
            // announced to the session connection broker.
        }

        public IRemoteDesktopSession ActiveRemoteDesktopSession
        {
            get => RemoteDesktopView.TryGetActivePane(this.mainForm);
        }

        public ISession ActiveSession => this.ActiveRemoteDesktopSession;

        public bool IsConnected(InstanceLocator vmInstance)
        {
            return RemoteDesktopView.TryGetExistingPane(
                this.mainForm,
                vmInstance) != null;
        }

        public bool TryActivate(
            InstanceLocator vmInstance,
            out ISession session)
        {
            // Check if there is an existing session/pane.
            var rdpPane = RemoteDesktopView.TryGetExistingPane(
                this.mainForm,
                vmInstance);
            if (rdpPane != null)
            {
                // Pane found, activate.
                rdpPane.SwitchToDocument();
                session = rdpPane;
                return true;
            }
            else
            {
                session = null;
                return false;
            }
        }

        public IRemoteDesktopSession Connect(RdpConnectionTemplate template) 
        {
            var window = ToolWindow.GetWindow<RemoteDesktopView, RemoteDesktopViewModel>(this.serviceProvider);
            window.ViewModel.Instance = template.Instance;
            window.ViewModel.Server = template.Endpoint;
            window.ViewModel.Port = template.EndpointPort;
            window.ViewModel.Settings = template.Settings;

            var pane = window.Bind();
            window.Show();

            pane.Connect();

            return pane;
        }
    }
}
