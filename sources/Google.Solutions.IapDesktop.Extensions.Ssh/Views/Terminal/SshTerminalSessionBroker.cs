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
using Google.Solutions.IapDesktop.Extensions.Ssh.Services.Auth;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Views.Terminal
{
    public interface ISshTerminalSession : IDisposable
    {
        void Close();
    }

    public interface ISshTerminalSessionBroker : ISessionBroker
    {
        ISshTerminalSession ActiveSession { get; }

        Task<ISshTerminalSession> ConnectAsync(
            InstanceLocator vmInstance,
            IPEndPoint endpoint,
            AuthorizedKey authorizedKey);
    }

    [Service(typeof(ISshTerminalSessionBroker), ServiceLifetime.Singleton, ServiceVisibility.Global)]
    [ServiceCategory(typeof(ISessionBroker))]
    public class SshTerminalSessionBroker : ISshTerminalSessionBroker
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IMainForm mainForm;

        public SshTerminalSessionBroker(IServiceProvider serviceProvider)
        {
            this.mainForm = serviceProvider.GetService<IMainForm>();
            this.serviceProvider = serviceProvider;

            // NB. The ServiceCategory attribute causes this class to be 
            // announced to the global connection broker.
        }

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public ISshTerminalSession ActiveSession
        {
            get => SshTerminalPane.TryGetActivePane(this.mainForm);
        }

        public bool IsConnected(InstanceLocator vmInstance)
        {
            return SshTerminalPane.TryGetExistingPane(
                this.mainForm,
                vmInstance) != null;
        }

        public bool TryActivate(InstanceLocator vmInstance)
        {
            // Check if there is an existing session/pane.
            var pane = SshTerminalPane.TryGetExistingPane(
                this.mainForm,
                vmInstance);
            if (pane != null)
            {
                // Pane found, activate.
                pane.ShowWindow();
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<ISshTerminalSession> ConnectAsync(
            InstanceLocator vmInstance,
            IPEndPoint endpoint,
            AuthorizedKey authorizedKey)
        {
            var pane = new SshTerminalPane(
                this.serviceProvider,
                vmInstance,
                endpoint,
                authorizedKey);
            pane.ShowWindow(true);

            await pane.ConnectAsync()
                .ConfigureAwait(false);

            return pane;
        }
    }
}
