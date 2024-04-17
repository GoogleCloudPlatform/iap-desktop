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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.Platform.Dispatch;
using System;
using System.Diagnostics;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.App
{
    internal class AppProtocolContextFactory : IProtocolContextFactory
    {
        private readonly IConnectionSettingsService settingsService;
        private readonly IIapTransportFactory transportFactory;
        private readonly IWin32ProcessFactory processFactory;

        internal AppProtocolContextFactory(
            AppProtocol protocol,
            IIapTransportFactory transportFactory,
            IWin32ProcessFactory processFactory,
            IConnectionSettingsService settingsService)
        {
            this.Protocol = protocol.ExpectNotNull(nameof(protocol));
            this.transportFactory = transportFactory.ExpectNotNull(nameof(transportFactory));
            this.processFactory = processFactory.ExpectNotNull(nameof(processFactory));
            this.settingsService = settingsService.ExpectNotNull(nameof(settingsService));
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        /// <summary>
        /// Protocol that this factory applies to.
        /// </summary>
        public AppProtocol Protocol { get; }

        //---------------------------------------------------------------------
        // IProtocolFactory.
        //---------------------------------------------------------------------

        public Task<IProtocolContext> CreateContextAsync(
            IProtocolTarget target,
            uint flags,
            CancellationToken cancellationToken)
        {
            target.ExpectNotNull(nameof(target));

            if (this.Protocol.IsAvailable(target) &&
                target is IProjectModelInstanceNode instance)
            {
                var context = new AppProtocolContext(
                    this.Protocol,
                    this.transportFactory,
                    this.processFactory,
                    instance.Instance);

                var settings = this.settingsService
                    .GetConnectionSettings(instance)
                    .TypedCollection;

                //
                // Populate parameters from settings.
                //
                context.Parameters.PreferredUsername = settings.AppUsername.Value;
                context.Parameters.NetworkLevelAuthentication = settings.AppNetworkLevelAuthentication.Value;

                var contextFlags = (AppProtocolContextFlags)flags;
                if (contextFlags.HasFlag(AppProtocolContextFlags.TryUseRdpNetworkCredentials))
                {
                    //
                    // See if we have RDP credentials.
                    //
                    if (!string.IsNullOrEmpty(settings.RdpUsername.Value))
                    {
                        context.NetworkCredential = new NetworkCredential(
                            settings.RdpUsername.Value,
                            (SecureString?)settings.RdpPassword.Value,
                            settings.RdpDomain.Value);
                    }
                }
                else if (contextFlags != AppProtocolContextFlags.None)
                {
                    throw new ArgumentException("Unsupported flags: " + contextFlags);
                }

                return Task.FromResult<IProtocolContext>(context);
            }
            else
            {
                throw new ProtocolTargetException(
                    $"The protocol '{this.Protocol.Name}' can't be used for {target}",
                    HelpTopics.AppProtocols);
            }
        }

        public bool TryParse(Uri uri, out ProtocolTargetLocator? locator)
        {
            locator = null;
            return false;
        }
    }

    [Flags]
    public enum AppProtocolContextFlags : uint
    {
        None,

        /// <summary>
        /// Use RDP credentials as network credentials.
        /// </summary>
        TryUseRdpNetworkCredentials = 1,
    }
}
