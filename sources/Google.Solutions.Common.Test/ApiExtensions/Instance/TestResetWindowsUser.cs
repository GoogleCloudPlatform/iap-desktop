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
using Google.Solutions.Common.ApiExtensions.Instance;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test.Integration;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Test.Extensions
{
    [TestFixture]
    [Category("IntegrationTest")]
    [Category("Windows")]
    public class TestResetWindowsUser : FixtureBase
    {
        public ComputeService CreateComputeService(ICredential credential)
        {
            return new ComputeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential
            });
        }

        [Test]
        public async Task WhenUsernameIsSuperLong_ThenPasswordResetExceptionIsThrown(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var computeService = CreateComputeService(await credential);
            var username = "test" + Guid.NewGuid().ToString();
            try
            {
                await computeService.Instances.ResetWindowsUserAsync(
                    await testInstance,
                    username,
                    CancellationToken.None);
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
            var computeService = CreateComputeService(await credential);
            var username = "test" + Guid.NewGuid().ToString().Substring(20);

            // Use correct project, but wrong VM.
            var instanceRef = new InstanceLocator(
                TestProject.ProjectId,
                TestProject.Zone,
                "doesnotexist");
            try
            {
                await computeService.Instances.ResetWindowsUserAsync(
                    instanceRef,
                    username,
                    CancellationToken.None);
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
            var computeService = CreateComputeService(await credential);
            var username = "test" + Guid.NewGuid().ToString().Substring(20);
            var credentials = await computeService.Instances.ResetWindowsUserAsync(
                await testInstance,
                username,
                CancellationToken.None);

            Assert.AreEqual(username, credentials.UserName);
            Assert.IsEmpty(credentials.Domain);
            Assert.IsNotEmpty(credentials.Password);
        }

        [Test]
        public async Task WhenUserExists_ThenResetPasswordUpdatesPassword(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var computeService = CreateComputeService(await credential);
            var username = "existinguser";
            await computeService.Instances.ResetWindowsUserAsync(
                await testInstance,
                username,
                CancellationToken.None);
            var credentials = await computeService.Instances.ResetWindowsUserAsync(
                await testInstance,
                username,
                CancellationToken.None);

            Assert.AreEqual(username, credentials.UserName);
            Assert.IsEmpty(credentials.Domain);
            Assert.IsNotEmpty(credentials.Password);
        }

        [Test]
        public async Task WhenTokenSourceIsCanceled_ThenResetPasswordThrowsTaskCanceledException(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var computeService = CreateComputeService(await credential);
            var instanceLocator = await testInstance;
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                AssertEx.ThrowsAggregateException<TaskCanceledException>(
                    () => computeService.Instances.ResetWindowsUserAsync(
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
            var computeService = CreateComputeService(await credential);
            var instanceLocator = await testInstance;
            using (var cts = new CancellationTokenSource())
            {
                AssertEx.ThrowsAggregateException<PasswordResetException>(
                    () => computeService.Instances.ResetWindowsUserAsync(
                    instanceLocator,
                    "test" + Guid.NewGuid().ToString().Substring(20),
                    TimeSpan.FromMilliseconds(1),
                    cts.Token).Wait());
            }
        }
    }
}
