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
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Rdp;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session
{
    /// <summary>
    /// Connect to a VM by iap-rdp:/// URL, or activate an existing session
    /// if present.
    /// </summary>
    internal class ConnectRdpUrlCommand : ConnectInstanceCommandBase<IapRdpUrl>
    {
        private readonly ISessionContextFactory sessionContextFactory;
        private readonly IInstanceSessionBroker sessionFactory;
        private readonly ISessionBroker sessionBroker;

        public ConnectRdpUrlCommand(
            ISessionContextFactory sessionContextFactory,
            IInstanceSessionBroker sessionFactory,
            ISessionBroker sessionBroker)
            : base("Launch &RDP URL")
        {
            this.sessionContextFactory = sessionContextFactory;
            this.sessionFactory = sessionFactory;
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
            if (this.sessionBroker.TryActivate(url.Instance, out var activeSession))
            {
                //
                // There is an existing session, and it's now active.
                //
                Debug.Assert(activeSession != null);
                Debug.Assert(activeSession is IRdpSession);
            }
            else
            {
                //
                // Create new session.
                //
                var context = await this.sessionContextFactory
                    .CreateRdpSessionContextAsync(url, CancellationToken.None)
                    .ConfigureAwait(true);

                var session = await this.sessionFactory
                    .CreateSessionAsync(context)
                    .ConfigureAwait(true);

                Debug.Assert(session != null);
            }
        }
    }
}