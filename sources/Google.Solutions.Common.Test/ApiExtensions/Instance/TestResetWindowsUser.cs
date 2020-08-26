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

using Google.Apis.Compute.v1;
using Google.Apis.Compute.v1.Data;
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
        private InstancesResource instancesResource;

        [SetUp]
        public void SetUp()
        {
            this.instancesResource = TestProject.CreateComputeService().Instances;
        }

        [Test]
        public async Task WhenUsernameIsSuperLong_ThenPasswordResetExceptionIsThrown(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance)
        {
            var username = "test" + Guid.NewGuid().ToString();
            try
            {
                await this.instancesResource.ResetWindowsUserAsync(
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
        public async Task WhenInstanceDoesntExist_ThenPasswordResetExceptionIsThrown()
        {
            var username = "test" + Guid.NewGuid().ToString();

            // Use correct project, but wrong VM.
            var instanceRef = new InstanceLocator(
                TestProject .ProjectId,
                TestProject.Zone,
                "doesnotexist");
            try
            {
                await this.instancesResource.ResetWindowsUserAsync(
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
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance)
        {
            var username = "test" + Guid.NewGuid().ToString().Substring(20);
            var credentials = await this.instancesResource.ResetWindowsUserAsync(
                await testInstance,
                username,
                CancellationToken.None);

            Assert.AreEqual(username, credentials.UserName);
            Assert.IsEmpty(credentials.Domain);
            Assert.IsNotEmpty(credentials.Password);
        }

        [Test]
        public async Task WhenUserExists_ThenResetPasswordUpdatesPassword(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance)
        {
            var username = "existinguser";
            await this.instancesResource.ResetWindowsUserAsync(
                await testInstance,
                username,
                CancellationToken.None);
            var credentials = await this.instancesResource.ResetWindowsUserAsync(
                await testInstance,
                username,
                CancellationToken.None);

            Assert.AreEqual(username, credentials.UserName);
            Assert.IsEmpty(credentials.Domain);
            Assert.IsNotEmpty(credentials.Password);
        }

        [Test]
        public async Task WhenTokenSourceIsCanceled_ThenResetPasswordThrowsTaskCanceledException(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance)
        {
            var instanceLocator = await testInstance;
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                AssertEx.ThrowsAggregateException<TaskCanceledException>(
                    () => this.instancesResource.ResetWindowsUserAsync(
                    instanceLocator,
                    "test" + Guid.NewGuid().ToString().Substring(20),
                    cts.Token).Wait());
            }
        }

        [Test]
        public async Task WhenTimeoutElapses_ThenResetPasswordThrowsPasswordResetException(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance)
        {
            var instanceLocator = await testInstance;
            using (var cts = new CancellationTokenSource())
            {
                AssertEx.ThrowsAggregateException<PasswordResetException>(
                    () => this.instancesResource.ResetWindowsUserAsync(
                    instanceLocator,
                    "test" + Guid.NewGuid().ToString().Substring(20),
                    TimeSpan.FromMilliseconds(1),
                    cts.Token).Wait());
            }
        }
    }
}
