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

using Google.Apis.Util;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Auth.Gaia;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using System;

namespace Google.Solutions.IapDesktop.Application.Host
{
    public interface IUpdatePolicyFactory // TODO: Move to profile
    {
        /// <summary>
        /// Get policy for a specific track.
        /// </summary>
        IUpdatePolicy GetPolicy();
    }

    public class UpdatePolicyFactory : IUpdatePolicyFactory
    {
        private readonly IRepository<IApplicationSettings> settingsRepository;
        private readonly IAuthorization authorization;
        private readonly IInstall install;
        private readonly IClock clock;
        
        public UpdatePolicyFactory(
            IRepository<IApplicationSettings> settingsRepository,
            IAuthorization authorization,
            IInstall install, 
            IClock clock)
        {
            this.settingsRepository = settingsRepository.ExpectNotNull(nameof(settingsRepository));
            this.authorization = authorization.ExpectNotNull(nameof(authorization));
            this.install = install.ExpectNotNull(nameof(install));
            this.clock = clock.ExpectNotNull(nameof(clock));
        }

        public IUpdatePolicy GetPolicy()
        {
            //
            // Determine user's release track.
            //
            ReleaseTrack releaseTrack;
            if (!this.settingsRepository.GetSettings().IsUpdateCheckEnabled.BoolValue)
            {
                //
                // Updates are off, but still check for critical ones.
                //
                releaseTrack = ReleaseTrack.Critical;
            }
            else if (
                this.authorization.Session is IGaiaOidcSession session && (
                session.Email.EndsWith("@google.com", StringComparison.OrdinalIgnoreCase) ||
                session.Email.EndsWith(".altostrat.com", StringComparison.OrdinalIgnoreCase)))
            {
                //
                // Force-opt in internal domains to the canary track.
                //
                releaseTrack = ReleaseTrack.Canary;
            }
            else
            {
                releaseTrack = ReleaseTrack.Normal;
            }

            return new UpdatePolicy(
                this.install, 
                this.clock,
                releaseTrack);
        }
    }
}
