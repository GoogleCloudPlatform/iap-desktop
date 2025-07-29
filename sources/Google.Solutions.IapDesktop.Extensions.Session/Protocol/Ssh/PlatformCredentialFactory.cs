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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Crm;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Ssh.Cryptography;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh
{
    /// <summary>
    /// Factory for platform-managed SSH credentials. 
    /// </summary>
    public interface IPlatformCredentialFactory
    {
        /// <summary>
        /// Create a platform-managed key credential by using OS Login,
        /// instance- or project-metadata to authorize an asymmetric key.
        /// </summary>
        /// <returns></returns>
        Task<PlatformCredential> CreateCredentialAsync(
            InstanceLocator instance,
            IAsymmetricKeySigner signer,
            TimeSpan validity,
            string? preferredPosixUsername,
            KeyAuthorizationMethods methods,
            CancellationToken token);
    }

    [Flags]
    public enum KeyAuthorizationMethods
    {
        InstanceMetadata = 1,
        ProjectMetadata = 2,
        Oslogin = 4,
        All = 7
    }

    [Service(typeof(IPlatformCredentialFactory))]
    public class PlatformCredentialFactory : IPlatformCredentialFactory
    {
        private readonly IAuthorization authorization;
        private readonly IComputeEngineClient computeClient;
        private readonly IResourceManagerClient resourceManagerAdapter;
        private readonly IOsLoginProfile osLoginProfile;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public PlatformCredentialFactory(
            IAuthorization authorization,
            IComputeEngineClient computeClient,
            IResourceManagerClient resourceManagerAdapter,
            IOsLoginProfile osLoginProfile)
        {
            this.authorization = authorization.ExpectNotNull(nameof(authorization));
            this.computeClient = computeClient.ExpectNotNull(nameof(computeClient));
            this.resourceManagerAdapter = resourceManagerAdapter.ExpectNotNull(nameof(resourceManagerAdapter));
            this.osLoginProfile = osLoginProfile.ExpectNotNull(nameof(osLoginProfile));
        }

        internal string CreateUsernameForMetadata(string? preferredPosixUsername)
        {
            if (preferredPosixUsername != null)
            {
                if (!LinuxUser.IsValidUsername(preferredPosixUsername))
                {
                    throw new ArgumentException(
                        $"The username '{preferredPosixUsername}' is not a valid username");
                }
                else
                {
                    //
                    // Use the preferred username.
                    //
                    return preferredPosixUsername;
                }
            }
            else
            {
                // 
                // No preferred username provided, so derive one
                // from the user's username:
                //
                return LinuxUser.SuggestUsername(this.authorization);
            }
        }

        //---------------------------------------------------------------------
        // IPlatformCredentialFactory.
        //---------------------------------------------------------------------

        public async Task<PlatformCredential> CreateCredentialAsync(
            InstanceLocator instance,
            IAsymmetricKeySigner signer,
            TimeSpan validity,
            string? preferredPosixUsername,
            KeyAuthorizationMethods allowedMethods,
            CancellationToken token)
        {
            Precondition.ExpectNotNull(instance, nameof(instance));
            Precondition.ExpectNotNull(signer, nameof(signer));

            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(instance))
            {
                var metdataKeyProcessor = await InstanceMetadata.GetAsync(
                        this.computeClient,
                        this.resourceManagerAdapter,
                        instance,
                        token)
                    .ConfigureAwait(false);

                var osLoginEnabled = metdataKeyProcessor.IsOsLoginEnabled;

                ApplicationTraceSource.Log.TraceVerbose(
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
                        throw new OsLoginSkNotSupportedException(
                            $"The VM {instance.Name} requires a security key for " +
                                "authentication. This feature is currently not supported " +
                                "by IAP Desktop.",
                            HelpTopics.EnableOsLoginSecurityKeys);
                    }

                    return await this.osLoginProfile
                        .AuthorizeKeyAsync(
                            new ZoneLocator(instance.ProjectId, instance.Zone),
                            OsLoginSystemType.Linux,
                            signer,
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
                    return await metdataKeyProcessor
                        .AuthorizeKeyAsync(
                            signer,
                            validity,
                            CreateUsernameForMetadata(preferredPosixUsername),
                            allowedMethods,
                            this.authorization,
                            token)
                        .ConfigureAwait(false);
                }
            }
        }
    }
}
