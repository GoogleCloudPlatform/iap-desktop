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

using Google.Solutions.Common;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.Ssh.Auth;
using Google.Solutions.Ssh.Native;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Google.Solutions.Common.Test.Integration;
using Google.Apis.Compute.v1.Data;
using System.Threading;
using Google.Solutions.Common.ApiExtensions.Instance;
using Google.Solutions.Common.Util;

namespace Google.Solutions.Ssh.Test
{
    public abstract class SshFixtureBase : CommonFixtureBase
    {
        protected override IEnumerable<TraceSource> Sources => new[]
        {
            CommonTraceSources.Default,
            SshTraceSources.Default,
        };

        //---------------------------------------------------------------------
        // Handle tracking.
        //---------------------------------------------------------------------

        [SetUp]
        public void ClearOpenHandles()
        {
            HandleTable.Clear();
        }

        [TearDown]
        public void CheckOpenHandles()
        {
            HandleTable.DumpOpenHandles();
            Assert.AreEqual(0, HandleTable.HandleCount);
        }

        //---------------------------------------------------------------------
        // Helper methods.
        //---------------------------------------------------------------------

        protected static SshSession CreateSession()
        {
            var session = new SshSession();
            session.SetTraceHandler(
                LIBSSH2_TRACE.SOCKET |
                    LIBSSH2_TRACE.ERROR |
                    LIBSSH2_TRACE.CONN |
                    LIBSSH2_TRACE.AUTH |
                    LIBSSH2_TRACE.KEX |
                    LIBSSH2_TRACE.SFTP,
                Console.WriteLine);

            session.Timeout = TimeSpan.FromSeconds(5);

            return session;
        }

        protected static async Task<IPAddress> GetPublicIpAddressForInstanceAsync(
            InstanceLocator instanceLocator)
        {
            using (var service = TestProject.CreateComputeService())
            {
                var instance = await service
                    .Instances.Get(
                            instanceLocator.ProjectId,
                            instanceLocator.Zone,
                            instanceLocator.Name)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                var ip = instance
                    .NetworkInterfaces
                    .EnsureNotNull()
                    .Where(nic => nic.AccessConfigs != null)
                    .SelectMany(nic => nic.AccessConfigs)
                    .EnsureNotNull()
                    .Where(accessConfig => accessConfig.Type == "ONE_TO_ONE_NAT")
                    .Select(accessConfig => accessConfig.NatIP)
                    .FirstOrDefault();
                return IPAddress.Parse(ip);
            }
        }

        protected static async Task<IPEndPoint> GetPublicSshEndpointAsync(
            InstanceLocator instance)
        {
            return new IPEndPoint(
                await GetPublicIpAddressForInstanceAsync(instance)
                    .ConfigureAwait(false),
                22);
        }

        private readonly static IDictionary<string, ISshAuthenticator> cachedAuthenticators =
            new Dictionary<string, ISshAuthenticator>();

        /// <summary>
        /// Create an authenticator for a given key type, minimizing
        /// server rountrips.
        /// </summary>
        protected static async Task<ISshAuthenticator> CreateEphemeralAuthenticatorForInstanceAsync(
            InstanceLocator instanceLocator,
            SshKeyType keyType)
        {
            var username = $"testuser";
            var cacheKey = $"{instanceLocator}|{username}|{keyType}";

            if (!cachedAuthenticators.ContainsKey(cacheKey))
            {
                //
                // Create a set of keys for this user/instance.
                //
                // N.B. We are replacing all existing keys for this
                // user. Therefore, upload keys for all key types.
                // 
                var keys = Enum.GetValues(typeof(SshKeyType))
                    .Cast<SshKeyType>()
                    .Select(t => SshKeyPair.NewEphemeralKeyPair(t))
                    .ToList();

                var metadataEntry = string.Join("\n", keys.Select(
                    k => $"{username}:{k.Type} {k.PublicKeyString} {username}"));

                using (var service = TestProject.CreateComputeService())
                {
                    await service.Instances
                        .AddMetadataAsync(
                            instanceLocator,
                            new Metadata()
                            {
                                Items = new[]
                                {
                                new Metadata.ItemsData()
                                {
                                    Key = "ssh-keys",
                                    Value = metadataEntry
                                }
                                }
                            },
                            CancellationToken.None)
                    .ConfigureAwait(false);

                }

                foreach (var key in keys)
                {
                    cachedAuthenticators[cacheKey] = new SshSingleFactorAuthenticator(username, key);
                }
            }

            return cachedAuthenticators[cacheKey];
        }
    }
}
