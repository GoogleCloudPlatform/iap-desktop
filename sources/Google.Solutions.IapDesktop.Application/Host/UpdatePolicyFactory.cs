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

namespace Google.Solutions.IapDesktop.Application.Host
{
    public interface IUpdatePolicyFactory
    {
        /// <summary>
        /// Get policy for a specific track.
        /// </summary>
        IUpdatePolicy GetPolicy(ReleaseTrack followedTrack);
    }

    public class UpdatePolicyFactory : IUpdatePolicyFactory
    {
        private readonly IInstall install;
        private readonly IAuthorization authorization;
        private readonly IClock clock;

        public UpdatePolicyFactory(
            IInstall install, 
            IAuthorization authorization, 
            IClock clock)
        {
            this.install = install.ExpectNotNull(nameof(install));
            this.authorization = authorization.ExpectNotNull(nameof(authorization));
            this.clock = clock.ExpectNotNull(nameof(clock));
        }

        public IUpdatePolicy GetPolicy(ReleaseTrack followedTrack)
        {
            return new UpdatePolicy(
                this.install, 
                this.authorization, 
                this.clock,
                followedTrack);
        }
    }
}
