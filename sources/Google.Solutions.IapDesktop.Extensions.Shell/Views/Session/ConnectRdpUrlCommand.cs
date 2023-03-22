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

using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Rdp;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Session
{
    /// <summary>
    /// Connect to a VM by iap-rdp:/// URL, or activate an existing session
    /// if present.
    /// </summary>
    internal class ConnectRdpUrlCommand : ConnectInstanceCommandBase<IapRdpUrl>
    {
        private readonly Service<IRdpConnectionService> connectionService;
        private readonly Service<IRemoteDesktopSessionBroker> sessionBroker;

        public ConnectRdpUrlCommand(
            Service<IRdpConnectionService> connectionService,
            Service<IRemoteDesktopSessionBroker> sessionBroker)
            : base("Launch &RDP URL")
        {
            this.connectionService = connectionService;
            this.sessionBroker = sessionBroker;
        }

        protected override bool IsAvailable(IapRdpUrl url)
        {
            return url != null;
        }

        protected override bool IsEnabled(IapRdpUrl url)
        {
            return url != null;
        }

        public override async Task ExecuteAsync(IapRdpUrl url)
        {
            if (this.sessionBroker
                .GetInstance()
                .TryActivate(url.Instance, out var activeSession))
            {
                //
                // There is an existing session, and it's now active.
                //
                Debug.Assert(activeSession != null);
                Debug.Assert(activeSession is IRemoteDesktopSession);
            }
            else
            {
                //
                // Create new session.
                //
                var template = await this.connectionService
                    .GetInstance()
                    .PrepareConnectionAsync(url)
                    .ConfigureAwait(true);

                var session = this.sessionBroker
                    .GetInstance()
                    .Connect(template);

                Debug.Assert(session != null);
            }
        }
    }
}