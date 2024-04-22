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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Crm;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Cryptography;
using Google.Solutions.Ssh.Native;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Protocol.Ssh
{
    [TestFixture]
    [UsesCloudResources]
    public class TestPlatformCredentialAuthentication
    {
        private static PlatformCredentialFactory CreateCredentialFactory(
            IAuthorization authorization)
        {
            return new PlatformCredentialFactory(
                authorization,
                new ComputeEngineClient(
                    ComputeEngineClient.CreateEndpoint(),
                    authorization,
                    TestProject.UserAgent),
                new ResourceManagerClient(
                    ResourceManagerClient.CreateEndpoint(),
                    authorization,
                    TestProject.UserAgent),
                new OsLoginProfile(
                    new OsLoginClient(
                        OsLoginClient.CreateEndpoint(),
                        authorization,
                        TestProject.ApiKey,
                        TestProject.UserAgent),
                    authorization));
        }

        private static async Task<IPAddress> GetPublicAddressFromLocatorAsync(
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
                return instance.PublicAddress();
            }
        }

        private static async Task VerifyCredentialAsync(
            InstanceLocator instance,
            IAsymmetricKeyCredential credential,
            IKeyboardInteractiveHandler handler)
        {
            var ipAddress = await GetPublicAddressFromLocatorAsync(instance)
                .ConfigureAwait(false);

            using (var connection = new SshConnection(
                new IPEndPoint(ipAddress, 22),
                credential,
                handler,
                new SynchronizationContext()))
            {
                await connection.ConnectAsync()
                    .ConfigureAwait(false);

                using (var fs = await connection
                    .OpenFileSystemAsync()
                    .ConfigureAwait(false))
                {
                    await fs
                        .ListFilesAsync("/")
                        .ConfigureAwait(false);
                }
            }
        }

        //---------------------------------------------------------------------
        // Gaia session.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUsingGaiaSessionAndInRole_ThenAuthenticationWithMetadataSucceeds(
            [Values(SshKeyType.Rsa3072, SshKeyType.EcdsaNistp256)] SshKeyType keyType,
            [LinuxInstance] ResourceTask<InstanceLocator> instance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> authorization)
        {
            using (var key = AsymmetricKeySigner.CreateEphemeral(keyType))
            using (var credential = await CreateCredentialFactory(await authorization)
                .CreateCredentialAsync(
                    await instance,
                    key,
                    TimeSpan.FromHours(1),
                    "preferred",
                    KeyAuthorizationMethods.All & ~KeyAuthorizationMethods.ProjectMetadata,
                    CancellationToken.None)
                .ConfigureAwait(false))
            {
                Assert.AreEqual("preferred", credential.Username);
                Assert.AreEqual(KeyAuthorizationMethods.InstanceMetadata, credential.AuthorizationMethod);

                await
                    VerifyCredentialAsync(
                        await instance,
                        credential,
                        new Mock<IKeyboardInteractiveHandler>().Object)
                    .ConfigureAwait(false);
            }
        }

        [Test]
        public async Task WhenUsingGaiaSessionAndInRole_ThenAuthenticationWithOsLoginSucceeds(
            [Values(SshKeyType.Rsa3072)] SshKeyType keyType,
            [LinuxInstance(EnableOsLogin = true)] ResourceTask<InstanceLocator> instance,
            [Credential(Roles = new [] {
                PredefinedRole.ComputeViewer,
                PredefinedRole.OsLogin})] ResourceTask<IAuthorization> authorization)
        {
            //
            // NB. Authentication can fail if we do two connection attempts in quick
            // succession using different keys (a limitation of the guest environment).
            // Therefore use a single key only.
            // 
            using (var key = AsymmetricKeySigner.CreateEphemeral(keyType))
            using (var credential = await CreateCredentialFactory(await authorization)
                .CreateCredentialAsync(
                    await instance,
                    key,
                    TimeSpan.FromHours(1),
                    "preferred",
                    KeyAuthorizationMethods.All,
                    CancellationToken.None)
                .ConfigureAwait(false))
            {
                StringAssert.StartsWith("sa_", credential.Username);
                Assert.AreEqual(KeyAuthorizationMethods.Oslogin, credential.AuthorizationMethod);

                await
                    VerifyCredentialAsync(
                        await instance,
                        credential,
                        new Mock<IKeyboardInteractiveHandler>().Object)
                    .ConfigureAwait(false);
            }
        }

        [Test]
        public async Task WhenUsingGaiaSessionAndNotInRole_ThenAuthenticationWithOsLoginFails(
            [Values(SshKeyType.Rsa3072, SshKeyType.EcdsaNistp256)] SshKeyType keyType,
            [LinuxInstance(EnableOsLogin = true)] ResourceTask<InstanceLocator> instance,
            [Credential(Roles = new [] {
                PredefinedRole.ComputeViewer,
                PredefinedRole.OsLogin})] ResourceTask<IAuthorization> authorization)
        {
            using (var key = AsymmetricKeySigner.CreateEphemeral(keyType))
            using (var credential = await CreateCredentialFactory(await authorization)
                .CreateCredentialAsync(
                    await instance,
                    key,
                    TimeSpan.FromHours(1),
                    "preferred",
                    KeyAuthorizationMethods.All,
                    CancellationToken.None)
                .ConfigureAwait(false))
            {
                StringAssert.StartsWith("sa_", credential.Username);
                Assert.AreEqual(KeyAuthorizationMethods.Oslogin, credential.AuthorizationMethod);

                var instanceLocator = await instance;
                ExceptionAssert.ThrowsAggregateException<Libssh2Exception>(
                    "Username/PublicKey combination invalid",
                    () => VerifyCredentialAsync(
                        instanceLocator,
                        credential,
                        new Mock<IKeyboardInteractiveHandler>().Object).Wait());
            }
        }

        //---------------------------------------------------------------------
        // Workforce session.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUsingWorkforceSessionAndInRole_ThenAuthenticationWithMetadataSucceeds(
            [Values(SshKeyType.Rsa3072, SshKeyType.EcdsaNistp256)] SshKeyType keyType,
            [LinuxInstance] ResourceTask<InstanceLocator> instance,
            [Credential(
                Type = PrincipalType.WorkforceIdentity,
                Role = PredefinedRole.ComputeInstanceAdminV1)]
                ResourceTask<IAuthorization> authorization)
        {
            using (var key = AsymmetricKeySigner.CreateEphemeral(keyType))
            using (var credential = await CreateCredentialFactory(await authorization)
                .CreateCredentialAsync(
                    await instance,
                    key,
                    TimeSpan.FromHours(1),
                    "preferred",
                    KeyAuthorizationMethods.All & ~KeyAuthorizationMethods.ProjectMetadata,
                    CancellationToken.None)
                .ConfigureAwait(false))
            {
                Assert.AreEqual("preferred", credential.Username);
                Assert.AreEqual(KeyAuthorizationMethods.InstanceMetadata, credential.AuthorizationMethod);

                await
                    VerifyCredentialAsync(
                        await instance,
                        credential,
                        new Mock<IKeyboardInteractiveHandler>().Object)
                    .ConfigureAwait(false);
            }
        }

        [Test]
        public async Task WhenUsingWorkforceSessionAndInRole_ThenAuthenticationWithOsLoginSucceeds(
            [Values(SshKeyType.Rsa3072, SshKeyType.EcdsaNistp256)] SshKeyType keyType,
            [LinuxInstance(EnableOsLogin = true)] ResourceTask<InstanceLocator> instance,
            [Credential(
                Type = PrincipalType.WorkforceIdentity,
                Roles = new [] {
                    PredefinedRole.ComputeViewer,
                    PredefinedRole.OsLogin,
                    PredefinedRole.ServiceUsageConsumer})]
                ResourceTask<IAuthorization> authorization)
        {
            using (var key = AsymmetricKeySigner.CreateEphemeral(keyType))
            using (var credential = await CreateCredentialFactory(await authorization)
                .CreateCredentialAsync(
                    await instance,
                    key,
                    TimeSpan.FromHours(1),
                    "preferred",
                    KeyAuthorizationMethods.All,
                    CancellationToken.None)
                .ConfigureAwait(false))
            {
                Assert.AreEqual(
                    (await authorization).Session.Username,
                    credential.Username);
                Assert.AreEqual(KeyAuthorizationMethods.Oslogin, credential.AuthorizationMethod);

                await
                    VerifyCredentialAsync(
                        await instance,
                        credential,
                        new Mock<IKeyboardInteractiveHandler>().Object)
                    .ConfigureAwait(false);
            }
        }
    }
}
