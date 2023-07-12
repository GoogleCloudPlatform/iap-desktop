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
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Host.Adapters;
using System;

namespace Google.Solutions.IapDesktop.Application.Host
{
    /// <summary>
    /// Policy that determines how often to check for updates,
    /// and which updates to apply.
    /// </summary>
    internal class UpdatePolicy
    {
        /// <summary>
        /// Determines how often update checks are performed. 
        /// A higher number implies a slower pace of updates.
        /// </summary>
        internal const int DaysBetweenUpdateChecks = 10;

        private readonly IInstall install;
        private readonly IClock clock;

        public UpdatePolicy(
            IAuthorization authorization,
            IInstall install,
            IClock clock)
        {
            authorization.ExpectNotNull(nameof(authorization));

            this.install = install.ExpectNotNull(nameof(install));
            this.clock = clock.ExpectNotNull(nameof(clock));

            //
            // Only install critical and normal updates by default.
            //
            this.FollowedTracks = ReleaseTrack.Critical | ReleaseTrack.Normal;

            //
            // Let internal/consumer domains follow the rapid track too.
            //
            if (authorization.Email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase) ||
                authorization.Email.EndsWith("@google.com", StringComparison.OrdinalIgnoreCase) ||
                authorization.Email.EndsWith(".joonix.net", StringComparison.OrdinalIgnoreCase) ||
                authorization.Email.EndsWith(".altostrat.com", StringComparison.OrdinalIgnoreCase))
            {
                this.FollowedTracks |= ReleaseTrack.Rapid;
            }
        }

        /// <summary>
        /// Release track followed by this user/machine.
        /// </summary>
        public ReleaseTrack FollowedTracks { get; }

        /// <summary>
        /// Determine which release track a release belongs to.
        /// </summary>
        public ReleaseTrack GetReleaseTrack(IGitHubRelease release)
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
            else if (description.Contains("[track:rapid]"))
            {
                return ReleaseTrack.Rapid;
            }
            else if (description.Contains("[track:optional]"))
            {
                return ReleaseTrack.Optional;
            }
            else
            {
                return ReleaseTrack.Normal;
            }
        }

        /// <summary>
        /// Determine if the user should be advised to install an update.
        /// </summary>
        public bool IsUpdateAdvised(IGitHubRelease release) //TODO: Rename to IRelease
        {
            Precondition.ExpectNotNull(release, nameof(release));

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
                return this.FollowedTracks.HasFlag(GetReleaseTrack(release));
            }
        }

        /// <summary>
        /// Determine if an update check should be performed.
        /// </summary>
        public bool IsUpdateCheckDue(DateTime lastCheck)
        {
            return (this.clock.UtcNow - lastCheck).TotalDays >= DaysBetweenUpdateChecks;
        }
    }

    /// <summary>
    /// Available release tracks.
    /// </summary>
    [Flags]
    internal enum ReleaseTrack : uint
    {
        /// <summary>
        /// Critical security updates.
        /// </summary>
        Critical = 1,

        /// <summary>
        /// Normal feature updates (later stage).
        /// </summary>
        Normal = 2,

        /// <summary>
        /// Normal feature updates (early stage).
        /// </summary>
        Rapid = 4,

        /// <summary>
        /// Optional feature updates that might only be relevant to few.
        /// </summary>
        Optional = 8,
    }
}
