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

using Google.Apis.Util;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.Ssh.Auth;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh
{
    [Service(typeof(IKeyAuthorizationService))]
    public class KeyAuthorizationService : IKeyAuthorizationService
    {

        private readonly IAuthorizationSource authorizationSource;
        private readonly IComputeEngineAdapter computeEngineAdapter;
        private readonly IResourceManagerAdapter resourceManagerAdapter;
        private readonly IOsLoginService osLoginService;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public KeyAuthorizationService(
            IAuthorizationSource authorizationSource,
            IComputeEngineAdapter computeEngineAdapter,
            IResourceManagerAdapter resourceManagerAdapter,
            IOsLoginService osLoginService)
        {
            this.authorizationSource = authorizationSource.ThrowIfNull(nameof(authorizationSource));
            this.computeEngineAdapter = computeEngineAdapter.ThrowIfNull(nameof(computeEngineAdapter));
            this.resourceManagerAdapter = resourceManagerAdapter.ThrowIfNull(nameof(resourceManagerAdapter));
            this.osLoginService = osLoginService.ThrowIfNull(nameof(osLoginService));
        }

        //---------------------------------------------------------------------
        // IPublicKeyService.
        //---------------------------------------------------------------------

        public async Task<AuthorizedKeyPair> AuthorizeKeyAsync(
            InstanceLocator instance,
            ISshKeyPair key,
            TimeSpan validity,
            string preferredPosixUsername,
            KeyAuthorizationMethods allowedMethods,
            CancellationToken token)
        {
            Utilities.ThrowIfNull(instance, nameof(instance));
            Utilities.ThrowIfNull(key, nameof(key));

            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(instance))
            {
                var metdataKeyProcessor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                        this.computeEngineAdapter,
                        this.resourceManagerAdapter,
                        instance,
                        token)
                    .ConfigureAwait(false);

                var osLoginEnabled = metdataKeyProcessor.IsOsLoginEnabled;

                ApplicationTraceSources.Default.TraceVerbose(
                    "OS Login status for {0}: {1}", instance, osLoginEnabled);

                if (osLoginEnabled)
                {
                    //
                    // If OS Login is enabled, it has to be used. Any metadata keys
                    // are ignored.
                    //
                    if (!allowedMethods.HasFlag(KeyAuthorizationMethods.Oslogin))
                    {
                        throw new InvalidOperationException(
                            $"{instance.Name} requires OS Login");
                    }

                    if (metdataKeyProcessor.IsOsLoginWithSecurityKeyEnabled)
                    {
                        //
                        // VM requires security keys.
                        //
                        throw new NotImplementedException(
                            $"{instance.Name} requires a security key for authentication. " +
                            "This is currently not supported by IAP Desktop.");
                    }

                    //
                    // NB. It's cheaper to unconditionally push the key than
                    // to check for previous keys first.
                    // 
                    return await this.osLoginService.AuthorizeKeyPairAsync(
                            new ProjectLocator(instance.ProjectId),
                            OsLoginSystemType.Linux,
                            key,
                            validity,
                            token)
                        .ConfigureAwait(false);
                }
                else
                {
                    //
                    // Push public key to metadata. Let the processor
                    // figure out whether that's project or instance
                    // metadata.
                    //
                    return await metdataKeyProcessor.AuthorizeKeyPairAsync(
                            key,
                            validity,
                            preferredPosixUsername,
                            allowedMethods,
                            this.authorizationSource.Authorization,
                            token)
                        .ConfigureAwait(false);
                }
            }
        }
    }
}
