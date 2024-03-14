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

using Google.Solutions.IapDesktop.Application.Host;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Diagnostics
{
    /// <summary>
    /// Information about a release.
    /// </summary>
    public interface IRelease
    {
        /// <summary>
        /// Version number, if available.
        /// </summary>
        Version? TagVersion { get; }

        /// <summary>
        /// URL to website for this release.
        /// </summary>
        string DetailsUrl { get; }

        /// <summary>
        /// Markdown-formatted description.
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// Check if this is a canary release.
        /// </summary>
        bool IsCanaryRelease { get; }

        /// <summary>
        /// Survey associated with this release.
        /// </summary>
        IReleaseSurvey? Survey { get; }

        /// <summary>
        /// Get download URL of installer package.
        /// </summary>
        bool TryGetDownloadUrl(
            Architecture architecture, 
            out string? downloadUrl);
    }

    /// <summary>
    /// A survey.
    /// </summary>
    public interface IReleaseSurvey
    {
        /// <summary>
        /// Title of the survey.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Description.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// URL to survey.
        /// </summary>
        string Url { get; }
    }

    /// <summary>
    /// Feed for new and past releases.
    /// </summary>
    public interface IReleaseFeed
    {
        /// <summary>
        /// Look up the most recent release.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IRelease?> FindLatestReleaseAsync(
            CancellationToken cancellationToken);

        /// <summary>
        /// Look up the most recent release.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IRelease?> FindLatestReleaseAsync(
            ReleaseFeedOptions options,
            CancellationToken cancellationToken);

        /// <summary>
        /// List latest releases.
        /// </summary>
        Task<IEnumerable<IRelease>> ListReleasesAsync(
            ReleaseFeedOptions options,
            CancellationToken cancellationToken);
    }

    [Flags]
    public enum ReleaseFeedOptions
    {
        None = 0,
        IncludeCanaryReleases = 1,
    }
}
