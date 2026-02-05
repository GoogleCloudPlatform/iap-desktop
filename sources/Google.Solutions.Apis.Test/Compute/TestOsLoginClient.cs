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

using Google.Apis.Auth.OAuth2;
using Google.Apis.Compute.v1.Data;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Auth.Gaia;
using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Linq;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Test.Compute
{
    [TestFixture]
    [UsesCloudResources]
    public class TestOsLoginClient
    {
        public const string SampleKeyNistp256 =
            "AAAAE2VjZHNhLXNoYTItbmlzdHAyNTYAAAAIbmlzdHAyNTYAAABBBOAATK5b5Y" +
            "ERo8r80PGSNgH+fabpTTr1tSt3CcAXd1gk3E+f1vvPL/1MxYeGolwehAyTL8mP" +
            "kxxmyn0tRb5TGvM=";

        private static IAuthorization CreateGaiaAuthorizationWithMismatchedUser(
            string username,
            ICredential credential)
        {
            var session = new Mock<IGaiaOidcSession>();
            session.SetupGet(s => s.Username).Returns(username);
            session.SetupGet(s => s.Email).Returns(username);
            session.SetupGet(s => s.ApiCredential).Returns(credential);

            var authorization = new Mock<IAuthorization>();
            authorization.SetupGet(a => a.Session).Returns(session.Object);
            authorization.SetupGet(a => a.DeviceEnrollment)
                .Returns(TestProject.DisabledEnrollment);

            return authorization.Object;
        }

        private static async Task<Instance> GetInstanceDetails(
            IAuthorization authorization,
            InstanceLocator instanceLocator)
        {
            var computeClient = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                authorization,
                TestProject.UserAgent);

            return await computeClient
                .GetInstanceAsync(instanceLocator, CancellationToken.None)
                .ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // ImportSshPublicKey.
        //---------------------------------------------------------------------

        [Test]
        public async Task ImportSshPublicKey_WhenUsingWorkforceSession(
            [Credential(Type = PrincipalType.WorkforceIdentity)]
            ResourceTask<IAuthorization> authorization)
        {
            var client = new OsLoginClient(
                OsLoginClient.CreateEndpoint(),
                await authorization,
                TestProject.UserAgent);

            await ExceptionAssert
                .ThrowsAsync<OsLoginNotSupportedForWorkloadIdentityException>(
                    () => client.ImportSshPublicKeyAsync(
                        new ProjectLocator(TestProject.ProjectId),
                        "ssh-rsa blob",
                        TimeSpan.FromMinutes(1),
                        CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ImportSshPublicKey_WhenEmailAndCredentialMismatch(
            [Credential] ResourceTask<ICredential> credentialTask)
        {
            var authorization = CreateGaiaAuthorizationWithMismatchedUser(
                "x@gmail.com",
                await credentialTask);
            var client = new OsLoginClient(
                OsLoginClient.CreateEndpoint(),
                authorization,
                TestProject.UserAgent);

            await ExceptionAssert
                .ThrowsAsync<ResourceAccessDeniedException>(
                    () => client.ImportSshPublicKeyAsync(
                        new ProjectLocator(TestProject.ProjectId),
                        "ssh-rsa blob",
                        TimeSpan.FromMinutes(1),
                        CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ImportSshPublicKey_WhenEmailValid(
            [Credential(Role = PredefinedRole.ComputeViewer)]
            ResourceTask<IAuthorization> authorizationTask)
        {
            var client = new OsLoginClient(
                OsLoginClient.CreateEndpoint(),
                await authorizationTask,
                TestProject.UserAgent);

            var key = "ssh-rsa notarealkey-" + Guid.NewGuid().ToString();

            var profile = await client.ImportSshPublicKeyAsync(
                new ProjectLocator(TestProject.ProjectId),
                key,
                TimeSpan.FromMinutes(5),
                CancellationToken.None)
            .ConfigureAwait(false);

            var entry = profile.SshPublicKeys
                .Values
                .Where(k => k.Key.Contains(key))
                .FirstOrDefault();
            Assert.That(entry, Is.Not.Null);
        }

        //---------------------------------------------------------------------
        // GetLoginProfile.
        //---------------------------------------------------------------------

        [Test]
        public async Task GetLoginProfile_WhenUsingWorkforceSession(
            [Credential(Type = PrincipalType.WorkforceIdentity)]
            ResourceTask<IAuthorization> authorization)
        {
            var client = new OsLoginClient(
                OsLoginClient.CreateEndpoint(),
                await authorization,
                TestProject.UserAgent);

            await ExceptionAssert
                .ThrowsAsync<OsLoginNotSupportedForWorkloadIdentityException>(
                    () => client.GetLoginProfileAsync(
                        new ProjectLocator(TestProject.ProjectId),
                        CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task GetLoginProfile_WhenEmailAndCredentialMismatch(
            [Credential] ResourceTask<ICredential> credentialTask)
        {
            var authorization = CreateGaiaAuthorizationWithMismatchedUser(
                "x@gmail.com",
                await credentialTask);
            var client = new OsLoginClient(
                OsLoginClient.CreateEndpoint(),
                authorization,
                TestProject.UserAgent);

            await ExceptionAssert
                .ThrowsAsync<ResourceAccessDeniedException>(() => client.GetLoginProfileAsync(
                    new ProjectLocator(TestProject.ProjectId),
                    CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task GetLoginProfile_WhenEmailValid(
            [Credential(Role = PredefinedRole.ComputeViewer)]
            ResourceTask<IAuthorization> authorizationTask)
        {
            var client = new OsLoginClient(
                OsLoginClient.CreateEndpoint(),
                await authorizationTask,
                TestProject.UserAgent);

            var profile = await client
                .GetLoginProfileAsync(
                    new ProjectLocator(TestProject.ProjectId),
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(profile, Is.Not.Null);
        }

        //---------------------------------------------------------------------
        // DeleteSshPublicKey.
        //---------------------------------------------------------------------

        [Test]
        public async Task DeleteSshPublicKey_WhenUsingWorkforceSession(
            [Credential(Type = PrincipalType.WorkforceIdentity)]
            ResourceTask<IAuthorization> authorization)
        {
            var client = new OsLoginClient(
                OsLoginClient.CreateEndpoint(),
                await authorization,
                TestProject.UserAgent);

            await ExceptionAssert
                .ThrowsAsync<OsLoginNotSupportedForWorkloadIdentityException>(
                    () => client.DeleteSshPublicKeyAsync(
                        "fingerprint",
                        CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task DeleteSshPublicKey_WhenDeletingKeyTwice(
            [Credential(Role = PredefinedRole.OsLogin)]
            ResourceTask<IAuthorization> authorizationTask)
        {
            var client = new OsLoginClient(
                OsLoginClient.CreateEndpoint(),
                await authorizationTask,
                TestProject.UserAgent);

            var key = "ssh-rsa notarealkey-" + Guid.NewGuid().ToString();

            //
            // Import a key.
            //
            var profile = await client
                .ImportSshPublicKeyAsync(
                    new ProjectLocator(TestProject.ProjectId),
                    key,
                    TimeSpan.FromMinutes(5),
                    CancellationToken.None)
                .ConfigureAwait(false);

            var entry = profile.SshPublicKeys
                .Values
                .Where(k => k.Key.Contains(key))
                .FirstOrDefault();
            Assert.That(entry, Is.Not.Null);

            //
            // Delete key twice.
            //
            await client
                .DeleteSshPublicKeyAsync(
                    entry.Fingerprint,
                    CancellationToken.None)
                .ConfigureAwait(false);
            await client.DeleteSshPublicKeyAsync(
                    entry.Fingerprint,
                    CancellationToken.None)
                .ConfigureAwait(false);

            //
            // Check that it's gone.
            //
            profile = await client
                .GetLoginProfileAsync(
                    new ProjectLocator(TestProject.ProjectId),
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(profile.SshPublicKeys
                .EnsureNotNull()
                .Any(k => k.Key.Contains(key)), Is.False);
        }

        [Test]
        public async Task DeleteSshPublicKey_WhenDeletingNonexistingKey(
            [Credential(Role = PredefinedRole.ServiceUsageConsumer)]
            ResourceTask<IAuthorization> authorizationTask)
        {
            var client = new OsLoginClient(
                OsLoginClient.CreateEndpoint(),
                await authorizationTask,
                TestProject.UserAgent);

            await client.DeleteSshPublicKeyAsync(
                    "nonexisting",
                    CancellationToken.None)
                .ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // SignPublicKey.
        //---------------------------------------------------------------------

        [Test]
        public async Task SignPublicKey_WorkforceIdentity_WhenUserNotInRole(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceTask,
            [Credential(
                Type = PrincipalType.WorkforceIdentity,
                Role = PredefinedRole.ComputeViewer)]
            ResourceTask<IAuthorization> authorizationTask)
        {
            var instance = await GetInstanceDetails(
                    await authorizationTask,
                    await instanceTask)
                .ConfigureAwait(false);

            var client = new OsLoginClient(
                OsLoginClient.CreateEndpoint(),
                await authorizationTask,
                TestProject.UserAgent);

            await ExceptionAssert
                .ThrowsAsync<ResourceAccessDeniedException>(() => client
                    .SignPublicKeyAsync(
                        new ZoneLocator(TestProject.ProjectId, TestProject.Zone),
                        instance.Id!.Value,
                        null,
                        $"ecdsa-sha2-nistp256 {SampleKeyNistp256}",
                        CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task SignPublicKey_WorkforceIdentity_WhenUserInRole(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceTask,
            [Credential(
                Type = PrincipalType.WorkforceIdentity,
                Roles = new [] {
                    PredefinedRole.ComputeViewer,
                    PredefinedRole.OsLogin,
                })]
            ResourceTask<IAuthorization> authorizationTask)
        {
            var instance = await GetInstanceDetails(
                    await authorizationTask,
                    await instanceTask)
                .ConfigureAwait(false);

            var client = new OsLoginClient(
                OsLoginClient.CreateEndpoint(),
                await authorizationTask,
                TestProject.UserAgent);

            var certifiedKey = await client
                .SignPublicKeyAsync(
                    new ZoneLocator(TestProject.ProjectId, TestProject.Zone),
                    instance.Id!.Value,
                    null,
                    $"ecdsa-sha2-nistp256 {SampleKeyNistp256}",
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(
                certifiedKey, Does.StartWith("ecdsa-sha2-nistp256-cert-v01@openssh.com"));
        }

        [Test]
        public async Task SignPublicKey_Gaia_WhenUserNotInRole(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceTask,
            [Credential(Role = PredefinedRole.ComputeViewer)]
            ResourceTask<IAuthorization> authorizationTask)
        {
            var instance = await GetInstanceDetails(
                    await authorizationTask,
                    await instanceTask)
                .ConfigureAwait(false);

            var client = new OsLoginClient(
                OsLoginClient.CreateEndpoint(),
                await authorizationTask,
                TestProject.UserAgent);


            await ExceptionAssert
                .ThrowsAsync<ResourceAccessDeniedException>(
                    () => client
                    .SignPublicKeyAsync(
                        new ZoneLocator(TestProject.ProjectId, TestProject.Zone),
                        instance.Id!.Value,
                        null,
                        $"ecdsa-sha2-nistp256 {SampleKeyNistp256}",
                        CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task SignPublicKey_Gaia_WhenUserInRole(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceTask,

            [Credential(Roles = new [] { 
                PredefinedRole.OsLogin, 
                PredefinedRole.ComputeViewer })]
            ResourceTask<IAuthorization> authorizationTask)
        {
            var instance = await GetInstanceDetails(
                    await authorizationTask,
                    await instanceTask)
                .ConfigureAwait(false);

            var client = new OsLoginClient(
                OsLoginClient.CreateEndpoint(),
                await authorizationTask,
                TestProject.UserAgent);

            await client
                .ProvisionPosixProfileAsync(
                    TestProject.Region,
                    null,
                    CancellationToken.None)
                .ConfigureAwait(false);

            var certifiedKey = await client
                .SignPublicKeyAsync(
                    new ZoneLocator(TestProject.ProjectId, TestProject.Zone),
                    instance.Id!.Value,
                    null,
                    $"ecdsa-sha2-nistp256 {SampleKeyNistp256}",
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(
                certifiedKey, Does.StartWith("ecdsa-sha2-nistp256-cert-v01@openssh.com"));
        }

        //---------------------------------------------------------------------
        // ProvisionPosixProfile.
        //---------------------------------------------------------------------

        [Test]
        public async Task ProvisionPosixProfile_WorkforceIdentity(
            [Credential(Type = PrincipalType.WorkforceIdentity)]
            ResourceTask<IAuthorization> authorizationTask)
        {
            var client = new OsLoginClient(
                OsLoginClient.CreateEndpoint(),
                await authorizationTask,
                TestProject.UserAgent);

            await client
                .ProvisionPosixProfileAsync(
                    TestProject.Region,
                    TestProject.ApiKey,
                    CancellationToken.None)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ProvisionPosixProfile_Gaia(
            [Credential(Role = PredefinedRole.StorageObjectViewer)]  // Unrelated role
            ResourceTask<IAuthorization> authorizationTask)
        {
            var client = new OsLoginClient(
                OsLoginClient.CreateEndpoint(),
                await authorizationTask,
                TestProject.UserAgent);

            await client
                .ProvisionPosixProfileAsync(
                    TestProject.Region,
                    TestProject.ApiKey,
                    CancellationToken.None)
                .ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // ListSecurityKeys.
        //---------------------------------------------------------------------

        [Test]
        public async Task ListSecurityKeys_WhenUsingWorkforceSession(
            [Credential(Type = PrincipalType.WorkforceIdentity)]
            ResourceTask<IAuthorization> authorization)
        {
            var client = new OsLoginClient(
                OsLoginClient.CreateEndpoint(),
                await authorization,
                TestProject.UserAgent);

            await ExceptionAssert
                .ThrowsAsync<OsLoginNotSupportedForWorkloadIdentityException>(
                    () => client.ListSecurityKeysAsync(
                        new ProjectLocator(TestProject.ProjectId),
                        CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ListSecurityKeys_WhenEmailAndCredentialMismatch(
            [Credential] ResourceTask<ICredential> credentialTask)
        {
            var authorization = CreateGaiaAuthorizationWithMismatchedUser(
                "x@gmail.com",
                await credentialTask);
            var client = new OsLoginClient(
                OsLoginClient.CreateEndpoint(),
                authorization,
                TestProject.UserAgent);

            await ExceptionAssert
                .ThrowsAsync<ResourceAccessDeniedException>(
                    () => client.ListSecurityKeysAsync(
                        new ProjectLocator(TestProject.ProjectId),
                        CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ListSecurityKeys_WhenEmailValid(
            [Credential(Role = PredefinedRole.ComputeViewer)]
            ResourceTask<IAuthorization> authorizationTask)
        {
            var client = new OsLoginClient(
                OsLoginClient.CreateEndpoint(),
                await authorizationTask,
                TestProject.UserAgent);

            var keys = await client
                .ListSecurityKeysAsync(
                    new ProjectLocator(TestProject.ProjectId),
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(keys, Is.Not.Null);
        }
    }
}
