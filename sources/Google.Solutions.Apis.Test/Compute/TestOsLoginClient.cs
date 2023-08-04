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
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
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
        //---------------------------------------------------------------------
        // ImportSshPublicKeyAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenEmailInvalid_ThenImportSshPublicKeyThrowsException(
            [Credential] ResourceTask<ICredential> credentialTask)
        {
            var authorization = new TemporaryAuthorization(
                "x@gmail.com",
                await credentialTask);
            var client = new OsLoginClient(
                OsLoginClient.CreateEndpoint(),
                authorization,
                TestProject.UserAgent);

            ExceptionAssert.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => client.ImportSshPublicKeyAsync(
                    new ProjectLocator(TestProject.ProjectId),
                    "ssh-rsa",
                    "blob",
                    TimeSpan.FromMinutes(1),
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenEmailValid_ThenImportSshPublicKeySucceeds(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> authorizationTask)
        {
            var client = new OsLoginClient(
                OsLoginClient.CreateEndpoint(),
                await authorizationTask,
                TestProject.UserAgent);

            var keyType = "ssh-rsa";
            var keyBlob = "notarealkey-" + Guid.NewGuid().ToString();

            var profile = await client.ImportSshPublicKeyAsync(
                new ProjectLocator(TestProject.ProjectId),
                keyType,
                keyBlob,
                TimeSpan.FromMinutes(5),
                CancellationToken.None)
            .ConfigureAwait(false);

            var key = profile.SshPublicKeys
                .Values
                .Where(k => k.Key.Contains(keyBlob))
                .FirstOrDefault();
            Assert.IsNotNull(key);
        }

        //---------------------------------------------------------------------
        // GetLoginProfileAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenEmailInvalid_ThenGetLoginProfileThrowsException(
            [Credential] ResourceTask<ICredential> credentialTask)
        {
            var authorization = new TemporaryAuthorization(
                "x@gmail.com",
                await credentialTask);
            var client = new OsLoginClient(
                OsLoginClient.CreateEndpoint(),
                authorization,
                TestProject.UserAgent);

            ExceptionAssert.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => client.GetLoginProfileAsync(
                    new ProjectLocator(TestProject.ProjectId),
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenEmailValid_ThenGetLoginProfileReturnsProfile(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> authorizationTask)
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

            Assert.IsNotNull(profile);
        }

        //---------------------------------------------------------------------
        // DeleteSshPublicKey.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenDeletingKeyTwice_ThenDeleteSucceeds(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> authorizationTask)
        {
            var client = new OsLoginClient(
                OsLoginClient.CreateEndpoint(),
                await authorizationTask,
                TestProject.UserAgent);

            var keyType = "ssh-rsa";
            var keyBlob = "notarealkey-" + Guid.NewGuid().ToString();

            //
            // Import a key.
            //
            var profile = await client.ImportSshPublicKeyAsync(
                    new ProjectLocator(TestProject.ProjectId),
                    keyType,
                    keyBlob,
                    TimeSpan.FromMinutes(5),
                    CancellationToken.None)
                .ConfigureAwait(false);

            var key = profile.SshPublicKeys
                .Values
                .Where(k => k.Key.Contains(keyBlob))
                .FirstOrDefault();
            Assert.IsNotNull(key);

            //
            // Delete key twice.
            //
            await client.DeleteSshPublicKeyAsync(
                    key.Fingerprint,
                    CancellationToken.None)
                .ConfigureAwait(false);
            await client.DeleteSshPublicKeyAsync(
                    key.Fingerprint,
                    CancellationToken.None)
                .ConfigureAwait(false);

            //
            // Check that it's gone.
            //
            profile = await client.GetLoginProfileAsync(
                    new ProjectLocator(TestProject.ProjectId),
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsFalse(profile.SshPublicKeys
                .EnsureNotNull()
                .Any(k => k.Key.Contains(keyBlob)));
        }

        [Test]
        public async Task WhenDeletingNonexistingKey_ThenDeleteSucceeds(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> authorizationTask)
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
    }
}
