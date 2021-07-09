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
using Google.Apis.Compute.v1;
using Google.Apis.Services;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Adapters
{
    [TestFixture]
    [Category("IntegrationTest")]
    [Category("Windows")]
    public class TestWindowsCredentialAdapter : ApplicationFixtureBase
    {
        [Test]
        public async Task WhenUsernameIsSuperLong_ThenPasswordResetExceptionIsThrown(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var adapter = new WindowsCredentialAdapter(new ComputeEngineAdapter(await credential));
            var username = "test" + Guid.NewGuid().ToString();
            
            try
            {
                await adapter.ResetWindowsUserAsync(
                        await testInstance,
                        username,
                        CancellationToken.None)
                    .ConfigureAwait(false);
                Assert.Fail();
            }
            catch (PasswordResetException e)
            {
                Assert.IsNotEmpty(e.Message);
            }
        }

        [Test]
        public async Task WhenInstanceDoesntExist_ThenPasswordResetExceptionIsThrown(
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var adapter = new WindowsCredentialAdapter(new ComputeEngineAdapter(await credential));

            var username = "test" + Guid.NewGuid().ToString().Substring(20);

            // Use correct project, but wrong VM.
            var instanceRef = new InstanceLocator(
                TestProject.ProjectId,
                TestProject.Zone,
                "doesnotexist");
            try
            {
                await adapter.ResetWindowsUserAsync(
                        instanceRef,
                        username,
                        CancellationToken.None)
                    .ConfigureAwait(false);
                Assert.Fail();
            }
            catch (PasswordResetException e)
            {
                Assert.IsNotEmpty(e.Message);
            }
        }

        [Test]
        public async Task WhenUserDoesntExist_ThenResetPasswordCreatesNewUser(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var adapter = new WindowsCredentialAdapter(new ComputeEngineAdapter(await credential));
            var username = "test" + Guid.NewGuid().ToString().Substring(20);

            var credentials = await adapter.ResetWindowsUserAsync(
                    await testInstance,
                    username,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(username, credentials.UserName);
            Assert.IsEmpty(credentials.Domain);
            Assert.IsNotEmpty(credentials.Password);
        }

        [Test]
        public async Task WhenUserExists_ThenResetPasswordUpdatesPassword(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var adapter = new WindowsCredentialAdapter(new ComputeEngineAdapter(await credential));

            var username = "existinguser";
            
            await adapter.ResetWindowsUserAsync(
                    await testInstance,
                    username,
                    CancellationToken.None)
                .ConfigureAwait(false);
            var credentials = await adapter.ResetWindowsUserAsync(
                    await testInstance,
                    username,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(username, credentials.UserName);
            Assert.IsEmpty(credentials.Domain);
            Assert.IsNotEmpty(credentials.Password);
        }

        [Test]
        public async Task WhenTokenSourceIsCanceled_ThenResetPasswordThrowsTaskCanceledException(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var adapter = new WindowsCredentialAdapter(new ComputeEngineAdapter(await credential));

            var instanceLocator = await testInstance;
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                AssertEx.ThrowsAggregateException<TaskCanceledException>(
                    () => adapter.ResetWindowsUserAsync(
                    instanceLocator,
                    "test" + Guid.NewGuid().ToString().Substring(20),
                    TimeSpan.FromMinutes(1),
                    cts.Token).Wait());
            }
        }

        [Test]
        public async Task WhenTimeoutElapses_ThenResetPasswordThrowsPasswordResetException(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var adapter = new WindowsCredentialAdapter(new ComputeEngineAdapter(await credential));

            var instanceLocator = await testInstance;
            using (var cts = new CancellationTokenSource())
            {
                AssertEx.ThrowsAggregateException<PasswordResetException>(
                    () => adapter.ResetWindowsUserAsync(
                    instanceLocator,
                    "test" + Guid.NewGuid().ToString().Substring(20),
                    TimeSpan.FromMilliseconds(1),
                    cts.Token).Wait());
            }
        }


        //---------------------------------------------------------------------
        // Permissions.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUserInRole_ThenIsGrantedPermissionToResetWindowsUserReturnsTrue(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;
            var adapter = new WindowsCredentialAdapter(new ComputeEngineAdapter(await credential));

            var result = await adapter.IsGrantedPermissionToResetWindowsUser(
                locator);

            Assert.IsTrue(result);
        }

        [Test]
        public async Task WhenUserNotInRole_ThenIsGrantedPermissionToResetWindowsUserReturnsFalse(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;
            var adapter = new WindowsCredentialAdapter(new ComputeEngineAdapter(await credential));

            var result = await adapter.IsGrantedPermissionToResetWindowsUser(
                locator);

            Assert.IsFalse(result);
        }

        [Test]
        public async Task WhenUserNotInInstanceAdminRole_ThenResetWindowsUserAsyncThrowsPasswordResetException(
            [WindowsInstance(ServiceAccount = InstanceServiceAccount.None)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;
            var adapter = new WindowsCredentialAdapter(new ComputeEngineAdapter(await credential));
            var username = "test" + Guid.NewGuid().ToString();

            AssertEx.ThrowsAggregateException<PasswordResetException>(
                () => adapter.ResetWindowsUserAsync(
                    locator,
                    username,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenInstanceHasNoServiceAccountAndUserInInstanceAdminRole_ThenResetWindowsUserAsyncSucceeds(
            [WindowsInstance(ServiceAccount = InstanceServiceAccount.None)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var adapter = new WindowsCredentialAdapter(new ComputeEngineAdapter(await credential));
            var username = "test" + Guid.NewGuid().ToString().Substring(20);

            var result = await adapter.ResetWindowsUserAsync(
                await testInstance,
                username,
                CancellationToken.None);
            Assert.IsNotNull(result.Password);
        }

        [Test]
        public async Task WhenInstanceHasServiceAccountAndUserInInstanceAdminRole_ThenResetWindowsUserAsyncThrowsPasswordResetException(
            [WindowsInstance(ServiceAccount = InstanceServiceAccount.ComputeDefault)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;
            var adapter = new WindowsCredentialAdapter(new ComputeEngineAdapter(await credential));
            var username = "test" + Guid.NewGuid().ToString();

            AssertEx.ThrowsAggregateException<PasswordResetException>(
                () => adapter.ResetWindowsUserAsync(
                    locator,
                    username,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenInstanceHasServiceAccountAndUserInInstanceAndServiceAccountUserAdminRole_ThenResetWindowsUserAsyncSucceeds(
            [WindowsInstance(ServiceAccount = InstanceServiceAccount.ComputeDefault)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Roles = new [] {
                PredefinedRole.ComputeInstanceAdminV1,
                PredefinedRole.ServiceAccountUser
            })] ResourceTask<ICredential> credential)
        {
            var adapter = new WindowsCredentialAdapter(new ComputeEngineAdapter(await credential));
            var username = "test" + Guid.NewGuid().ToString().Substring(20);

            var result = await adapter.ResetWindowsUserAsync(
                await testInstance,
                username,
                CancellationToken.None);
            Assert.IsNotNull(result.Password);
        }
    }
}
