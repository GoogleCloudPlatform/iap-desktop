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
using System;
using System.Diagnostics;

namespace Google.Solutions.IapDesktop.Application.Host
{
    /// <summary>
    /// Policy that determines how often to check for updates,
    /// and which updates to apply.
    /// </summary>
    public class UpdatePolicy
    {
        private readonly IInstall install;

        /// <summary>
        /// Release tracked followed by the user.
        /// </summary>
        public ReleaseTrack FollowedTracks { get; }

        /// <summary>
        /// Days to wait between two consecutive update checks.
        /// </summary>
        public ushort DaysBetweenUpdateChecks { get; }

        public UpdatePolicy(
            IInstall install,
            IAuthorization authorization,
            ReleaseTrack followedTracks)
        {
            Precondition.ExpectNotNull(authorization, nameof(authorization));
            Debug.Assert((followedTracks + 1).IsSingleFlag(), "Must include all lower tracks");

            this.install = install.ExpectNotNull(nameof(install));

            //
            // Force-opt in internal domains to the rapid track.
            //
            if (authorization.Session is IGaiaOidcSession session && (
                session.Email.EndsWith("@google.com", StringComparison.OrdinalIgnoreCase) ||
                session.Email.EndsWith(".altostrat.com", StringComparison.OrdinalIgnoreCase)))
            {
                followedTracks |= (ReleaseTrack.Critical | ReleaseTrack.Normal | ReleaseTrack.Rapid);
            }

            //
            // Determine how often update checks are performed. 
            // A higher number implies a slower pace of updates.
            //
            if (followedTracks.HasFlag(ReleaseTrack.Rapid))
            {
                this.DaysBetweenUpdateChecks = 1;
            }
            else
            {
                this.DaysBetweenUpdateChecks = 10;
            }

            this.FollowedTracks = followedTracks;
        }

        /// <summary>
        /// Determine which release track a release belongs to.
        /// </summary>
        internal ReleaseTrack GetReleaseTrack(IRelease release)
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
            else
            {
                return ReleaseTrack.Normal;
            }
        }

        /// <summary>
        /// Determine if the user should be advised to install an update.
        /// </summary>
        public bool IsUpdateAdvised(IRelease release)
        {
            Precondition.ExpectNotNull(release, nameof(release));

            Debug.Assert(this.FollowedTracks.HasFlag(ReleaseTrack.Critical));

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

        public bool IsUpdateCheckDue(
            IClock clock,
            DateTime lastCheck)
        {
            clock.ExpectNotNull(nameof(clock));
            Debug.Assert(lastCheck.Kind == DateTimeKind.Utc);

            return (clock.UtcNow - lastCheck).TotalDays >= this.DaysBetweenUpdateChecks;
        }
    }

    /// <summary>
    /// Available release tracks.
    /// </summary>
    [Flags]
    public enum ReleaseTrack : int
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

        _Default = Critical | Normal,
        _All = Critical | Normal | Rapid
    }
}
