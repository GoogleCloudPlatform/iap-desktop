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
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.Settings.Collection;
using System;
using System.Diagnostics;

namespace Google.Solutions.IapDesktop.Application.Profile
{
    /// <summary>
    /// Policy that determines how often to check for updates,
    /// and which updates to apply based on the user's identity 
    /// and settings.
    /// </summary>
    public interface IUpdatePolicy
    {
        /// <summary>
        /// Release tracked followed by the user.
        /// </summary>
        ReleaseTrack FollowedTrack { get; }

        /// <summary>
        /// Determine if the user should be advised to install an update.
        /// </summary>
        bool IsUpdateAdvised(IRelease release);

        /// <summary>
        /// Check if, given the current policy, an update check
        /// should be performed.
        /// </summary>
        bool IsUpdateCheckDue(DateTime lastCheck);

        /// <summary>
        /// Determine which release track a release belongs to.
        /// </summary>
        ReleaseTrack GetReleaseTrack(IRelease release);
    }

    public class UpdatePolicy : IUpdatePolicy
    {
        private readonly IRepository<IApplicationSettings> settingsRepository;
        private readonly IAuthorization authorization;
        private readonly IClock clock;
        private readonly IInstall install;

        public UpdatePolicy(
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

        /// <summary>
        /// Days to wait between two consecutive update checks.
        /// </summary>
        internal ushort DaysBetweenUpdateChecks
        {
            get
            {
                //
                // Determine how often update checks are performed. 
                // A higher number implies a slower pace of updates.
                //
                if (this.FollowedTrack.HasFlag(ReleaseTrack.Canary))
                {
                    return 1;
                }
                else
                {
                    return 10;
                }
            }
        }

        internal static ReleaseTrack GetReleaseTrackForRelease(IRelease release)
        {
            //
            // GitHub doesn't let us "tag" releases in a good way,
            // so we look for a tag in the description text.
            //
            var description = release.Description ?? string.Empty;

            if (description.Contains("[track:critical]"))
            {
                return ReleaseTrack.Critical;
            }
            else if (release.IsCanaryRelease)
            {
                return ReleaseTrack.Canary;
            }
            else
            {
                return ReleaseTrack.Normal;
            }
        }

        //---------------------------------------------------------------------
        // IUpdatePolicy.
        //---------------------------------------------------------------------

        public ReleaseTrack FollowedTrack
        {
            get
            {
                //
                // Determine user's release track.
                //
                if (!this.settingsRepository.GetSettings().IsUpdateCheckEnabled.Value)
                {
                    //
                    // Updates are off, but still check for critical ones.
                    //
                    return ReleaseTrack.Critical;
                }
                else if (
                    this.authorization.Session is IGaiaOidcSession session && (
                    session.Email.EndsWith("@google.com", StringComparison.OrdinalIgnoreCase) ||
                    session.Email.EndsWith(".altostrat.com", StringComparison.OrdinalIgnoreCase)))
                {
                    //
                    // Force-opt in internal domains to the canary track.
                    //
                    return ReleaseTrack.Canary;
                }
                else
                {
                    return ReleaseTrack.Normal;
                }
            }
        }

        public ReleaseTrack GetReleaseTrack(IRelease release)
        {
            return GetReleaseTrackForRelease(release);
        }

        public bool IsUpdateAdvised(IRelease release)
        {
            release.ExpectNotNull(nameof(release));

            if (release.TagVersion == null ||
                release.TagVersion.CompareTo(this.install.CurrentVersion) <= 0)
            {
                //
                // Installed version is up to date.
                //
                return false;
            }
            else
            {
                return this.FollowedTrack >= GetReleaseTrack(release);
            }
        }

        public bool IsUpdateCheckDue(DateTime lastCheck)
        {
            Debug.Assert(
                lastCheck.Kind == DateTimeKind.Utc ||
                lastCheck == DateTime.MinValue);

            return (this.clock.UtcNow - lastCheck).TotalDays >= this.DaysBetweenUpdateChecks;
        }
    }

    /// <summary>
    /// Type of release tracks.
    /// </summary>
    public enum ReleaseTrack : int
    {
        /// <summary>
        /// Includes critical security updates only.
        /// </summary>
        Critical = 0,

        /// <summary>
        /// Includes normal feature updates and critical security updates.
        /// </summary>
        Normal = 1,

        /// <summary>
        /// Includes early feature updates and critical security updates.
        /// </summary>
        Canary = 2,
    }
}
