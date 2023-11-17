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

using Google.Apis.Auth.OAuth2;
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
        public async Task WhenUsernameIsSuperLong_ThenPasswordResetExceptionIsThrown(
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
        public async Task WhenInstanceDoesntExist_ThenPasswordResetExceptionIsThrown(
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
        public async Task WhenUserDoesntExist_ThenCreateWindowsCredentialsCreatesNewUser(
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

            Assert.AreEqual(username, credentials.UserName);
            Assert.IsEmpty(credentials.Domain);
            Assert.IsNotEmpty(credentials.Password);
        }

        [Test]
        public async Task WhenAdminUserExists_ThenCreateWindowsCredentialsUpdatesPassword(
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

            Assert.AreEqual(username, credentials.UserName);
            Assert.IsEmpty(credentials.Domain);
            Assert.IsNotEmpty(credentials.Password);
        }

        [Test]
        public async Task WhenNormalUserExists_ThenCreateWindowsCredentialsUpdatesPasswordAndChangesType(
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

            Assert.AreEqual(username, credentials.UserName);
            Assert.IsEmpty(credentials.Domain);
            Assert.IsNotEmpty(credentials.Password);
        }

        [Test]
        public async Task WhenTokenSourceIsCanceled_ThenCreateWindowsCredentialsThrowsTaskCanceledException(
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

                ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                    () => adapter.CreateWindowsCredentialsAsync(
                    instanceLocator,
                    "test" + Guid.NewGuid().ToString().Substring(20),
                    UserFlags.AddToAdministrators,
                    TimeSpan.FromMinutes(1),
                    cts.Token).Wait());
            }
        }

        [Test]
        public async Task WhenTimeoutElapses_ThenCreateWindowsCredentialsThrowsPasswordResetException(
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
                ExceptionAssert.ThrowsAggregateException<WindowsCredentialCreationFailedException>(
                    () => adapter.CreateWindowsCredentialsAsync(
                    instanceLocator,
                    "test" + Guid.NewGuid().ToString().Substring(20),
                    UserFlags.AddToAdministrators,
                    TimeSpan.FromMilliseconds(1),
                    cts.Token).Wait());
            }
        }

        //---------------------------------------------------------------------
        // Permissions.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUserInRole_ThenIsGrantedPermissionToCreateWindowsCredentialsReturnsTrue(
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

            Assert.IsTrue(result);
        }

        [Test]
        public async Task WhenUserNotInRole_ThenIsGrantedPermissionToCreateWindowsCredentialsReturnsFalse(
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

            Assert.IsFalse(result);
        }

        [Test]
        public async Task WhenUserNotInInstanceAdminRole_ThenCreateWindowsCredentialsAsyncThrowsPasswordResetException(
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

            ExceptionAssert.ThrowsAggregateException<WindowsCredentialCreationFailedException>(
                () => adapter.CreateWindowsCredentialsAsync(
                    locator,
                    username,
                    UserFlags.AddToAdministrators,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenInstanceHasNoServiceAccountAndUserInInstanceAdminRole_ThenCreateWindowsCredentialsAsyncSucceeds(
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
            Assert.IsNotNull(result.Password);
        }

        [Test]
        public async Task WhenInstanceHasServiceAccountAndUserInInstanceAdminRole_ThenCreateWindowsCredentialsAsyncThrowsPasswordResetException(
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

            ExceptionAssert.ThrowsAggregateException<WindowsCredentialCreationFailedException>(
                () => adapter.CreateWindowsCredentialsAsync(
                    locator,
                    username,
                    UserFlags.AddToAdministrators,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenInstanceHasServiceAccountAndUserInInstanceAndServiceAccountUserAdminRole_ThenCreateWindowsCredentialsAsyncSucceeds(
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
            Assert.IsNotNull(result.Password);
        }
    }
}
