//
// Copyright 2019 Google LLC
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
using Google.Solutions.Apis.Locator;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Test.Compute
{
    [TestFixture]
    [UsesCloudResources]
    public class TestWindowsCredentialGenerator
    {
        [Test]
        public async Task CreateWindowsCredentials_WhenUsernameIsSuperLong_ThenPasswordResetExceptionIsThrown(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            var computeClient = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var adapter = new WindowsCredentialGenerator(computeClient);
            var username = "test" + Guid.NewGuid().ToString();

            try
            {
                await adapter
                    .CreateWindowsCredentialsAsync(
                        await testInstance,
                        username,
                        UserFlags.AddToAdministrators,
                        CancellationToken.None)
                    .ConfigureAwait(false);
                Assert.Fail();
            }
            catch (WindowsCredentialCreationFailedException e)
            {
                Assert.IsNotEmpty(e.Message);
            }
        }

        [Test]
        public async Task CreateWindowsCredentials_WhenInstanceDoesntExist_ThenPasswordResetExceptionIsThrown(
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            var computeClient = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var adapter = new WindowsCredentialGenerator(computeClient);
            var username = "test" + Guid.NewGuid().ToString().Substring(20);

            // Use correct project, but wrong VM.
            var instanceRef = new InstanceLocator(
                TestProject.ProjectId,
                TestProject.Zone,
                "doesnotexist");
            try
            {
                await adapter
                    .CreateWindowsCredentialsAsync(
                        instanceRef,
                        username,
                        UserFlags.AddToAdministrators,
                        CancellationToken.None)
                    .ConfigureAwait(false);
                Assert.Fail();
            }
            catch (WindowsCredentialCreationFailedException e)
            {
                Assert.IsNotEmpty(e.Message);
            }
        }

        [Test]
        public async Task CreateWindowsCredentials_WhenUserDoesntExist_ThenCreateWindowsCredentialsCreatesNewUser(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            var computeClient = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var adapter = new WindowsCredentialGenerator(computeClient);
            var username = "test" + Guid.NewGuid().ToString().Substring(20);

            var credentials = await adapter
                .CreateWindowsCredentialsAsync(
                    await testInstance,
                    username,
                    UserFlags.AddToAdministrators,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(credentials.UserName, Is.EqualTo(username));
            Assert.IsEmpty(credentials.Domain);
            Assert.IsNotEmpty(credentials.Password);
        }

        [Test]
        public async Task CreateWindowsCredentials_WhenAdminUserExists_ThenCreateWindowsCredentialsUpdatesPassword(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            var computeClient = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var adapter = new WindowsCredentialGenerator(computeClient);
            var username = "existinguser";

            await adapter
                .CreateWindowsCredentialsAsync(
                    await testInstance,
                    username,
                    UserFlags.AddToAdministrators,
                    CancellationToken.None)
                .ConfigureAwait(false);
            var credentials = await adapter
                .CreateWindowsCredentialsAsync(
                    await testInstance,
                    username,
                    UserFlags.None,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(credentials.UserName, Is.EqualTo(username));
            Assert.IsEmpty(credentials.Domain);
            Assert.IsNotEmpty(credentials.Password);
        }

        [Test]
        public async Task CreateWindowsCredentials_WhenNormalUserExists_ThenCreateWindowsCredentialsUpdatesPasswordAndChangesType(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            var computeClient = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var adapter = new WindowsCredentialGenerator(computeClient);
            var username = "existinguser";

            await adapter
                .CreateWindowsCredentialsAsync(
                    await testInstance,
                    username,
                    UserFlags.None,
                    CancellationToken.None)
                .ConfigureAwait(false);
            var credentials = await adapter
                .CreateWindowsCredentialsAsync(
                    await testInstance,
                    username,
                    UserFlags.AddToAdministrators,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(credentials.UserName, Is.EqualTo(username));
            Assert.IsEmpty(credentials.Domain);
            Assert.IsNotEmpty(credentials.Password);
        }

        [Test]
        public async Task CreateWindowsCredentials_WhenTokenSourceIsCanceled_ThenCreateWindowsCredentialsThrowsTaskCanceledException(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            var computeClient = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var adapter = new WindowsCredentialGenerator(computeClient);
            var instanceLocator = await testInstance;
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                await ExceptionAssert
                    .ThrowsAsync<OperationCanceledException>(() => adapter.CreateWindowsCredentialsAsync(
                        instanceLocator,
                        "test" + Guid.NewGuid().ToString().Substring(20),
                        UserFlags.AddToAdministrators,
                        TimeSpan.FromMinutes(1),
                        cts.Token))
                    .ConfigureAwait(false);
            }
        }

        [Test]
        public async Task CreateWindowsCredentials_WhenTimeoutElapses_ThenCreateWindowsCredentialsThrowsPasswordResetException(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            var computeClient = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var adapter = new WindowsCredentialGenerator(computeClient);

            var instanceLocator = await testInstance;
            using (var cts = new CancellationTokenSource())
            {
                await ExceptionAssert
                    .ThrowsAsync<WindowsCredentialCreationFailedException>(
                        () => adapter.CreateWindowsCredentialsAsync(
                            instanceLocator,
                            "test" + Guid.NewGuid().ToString().Substring(20),
                            UserFlags.AddToAdministrators,
                            TimeSpan.FromMilliseconds(1),
                            cts.Token))
                    .ConfigureAwait(false);
            }
        }

        //---------------------------------------------------------------------
        // Permissions.
        //---------------------------------------------------------------------

        [Test]
        public async Task IsGrantedPermissionToCreateWindowsCredentials_WhenUserInRole(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            var locator = await testInstance;
            var computeClient = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var adapter = new WindowsCredentialGenerator(computeClient);

            var result = await adapter
                .IsGrantedPermissionToCreateWindowsCredentialsAsync(locator)
                .ConfigureAwait(false);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsGrantedPermissionToCreateWindowsCredentialsWhenUserNotInRole_ThenIsGrantedPermissionToCreateWindowsCredentialsReturnsFalse(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var locator = await testInstance;
            var computeClient = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var adapter = new WindowsCredentialGenerator(computeClient);

            var result = await adapter
                .IsGrantedPermissionToCreateWindowsCredentialsAsync(locator)
                .ConfigureAwait(false);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task IsGrantedPermissionToCreateWindowsCredentialsWhenUserNotInRole_WhenUserNotInInstanceAdminRole(
            [WindowsInstance(ServiceAccount = InstanceServiceAccount.None)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var locator = await testInstance;
            var computeClient = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var adapter = new WindowsCredentialGenerator(computeClient);
            var username = "test" + Guid.NewGuid().ToString();

            await ExceptionAssert
                .ThrowsAsync<WindowsCredentialCreationFailedException>(
                    () => adapter.CreateWindowsCredentialsAsync(
                        locator,
                        username,
                        UserFlags.AddToAdministrators,
                        CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task IsGrantedPermissionToCreateWindowsCredentialsWhenUserNotInRole_WhenInstanceHasNoServiceAccountAndUserInInstanceAdminRole(
            [WindowsInstance(ServiceAccount = InstanceServiceAccount.None)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            var computeClient = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var adapter = new WindowsCredentialGenerator(computeClient);
            var username = "test" + Guid.NewGuid().ToString().Substring(20);

            var result = await adapter.CreateWindowsCredentialsAsync(
                    await testInstance,
                    username,
                    UserFlags.AddToAdministrators,
                    CancellationToken.None)
                .ConfigureAwait(false);
            Assert.That(result.Password, Is.Not.Null);
        }

        [Test]
        public async Task IsGrantedPermissionToCreateWindowsCredentialsWhenUserNotInRole_WhenInstanceHasServiceAccountAndUserInInstanceAdminRole(
            [WindowsInstance(ServiceAccount = InstanceServiceAccount.ComputeDefault)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            var locator = await testInstance;
            var computeClient = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var adapter = new WindowsCredentialGenerator(computeClient);
            var username = "test" + Guid.NewGuid().ToString();

            await ExceptionAssert
                .ThrowsAsync<WindowsCredentialCreationFailedException>(
                    () => adapter.CreateWindowsCredentialsAsync(
                        locator,
                        username,
                        UserFlags.AddToAdministrators,
                        CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task IsGrantedPermissionToCreateWindowsCredentialsWhenUserNotInRole_WhenInstanceHasServiceAccountAndUserInInstanceAndServiceAccountUserAdminRole(
            [WindowsInstance(ServiceAccount = InstanceServiceAccount.ComputeDefault)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Roles = new [] {
                PredefinedRole.ComputeInstanceAdminV1,
                PredefinedRole.ServiceAccountUser
            })] ResourceTask<IAuthorization> auth)
        {
            var computeClient = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var adapter = new WindowsCredentialGenerator(computeClient);
            var username = "test" + Guid.NewGuid().ToString().Substring(20);

            var result = await adapter
                .CreateWindowsCredentialsAsync(
                    await testInstance,
                    username,
                    UserFlags.AddToAdministrators,
                    CancellationToken.None)
                .ConfigureAwait(false);
            Assert.That(result.Password, Is.Not.Null);
        }
    }
}
