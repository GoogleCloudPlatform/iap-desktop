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
            [WindowsInstance] InstanceRequest testInstance)
        {
            await testInstance.AwaitReady();

            var username = "test" + Guid.NewGuid().ToString();
            try
            {
                await this.instancesResource.ResetWindowsUserAsync(
                    testInstance.Locator,
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
            [WindowsInstance] InstanceRequest testInstance)
        {
            await testInstance.AwaitReady();

            var username = "test" + Guid.NewGuid().ToString();

            // Use correct project, but wrong VM.
            var instanceRef = new InstanceLocator(
                testInstance.Locator.ProjectId,
                testInstance.Locator.Zone,
                testInstance.Locator.Name + "-x");
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
            [WindowsInstance] InstanceRequest testInstance)
        {
            await testInstance.AwaitReady();

            var username = "test" + Guid.NewGuid().ToString().Substring(20);
            var credentials = await this.instancesResource.ResetWindowsUserAsync(
                testInstance.Locator,
                username,
                CancellationToken.None);

            Assert.AreEqual(username, credentials.UserName);
            Assert.IsEmpty(credentials.Domain);
            Assert.IsNotEmpty(credentials.Password);
        }

        [Test]
        public async Task WhenUserExists_ThenResetPasswordUpdatesPassword(
            [WindowsInstance] InstanceRequest testInstance)
        {
            await testInstance.AwaitReady();

            var username = "existinguser";
            await this.instancesResource.ResetWindowsUserAsync(
                testInstance.Locator,
                username,
                CancellationToken.None);
            var credentials = await this.instancesResource.ResetWindowsUserAsync(
                testInstance.Locator,
                username,
                CancellationToken.None);

            Assert.AreEqual(username, credentials.UserName);
            Assert.IsEmpty(credentials.Domain);
            Assert.IsNotEmpty(credentials.Password);
        }
    }
}
