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

using Google.Solutions.Compute.Test.Env;
using Google.Solutions.Compute.Extensions;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Google.Apis.Compute.v1;

namespace Google.Solutions.Compute.Test.Extensions
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
            this.instancesResource = ComputeEngine.Connect().Service.Instances;
        }

        [Test]
        public async Task TestResetPasswordForSuperLongUsernameFails(
            [WindowsInstance] InstanceRequest testInstance)
        {
            await testInstance.AwaitReady();

            var username = "test" + Guid.NewGuid().ToString();
            try
            {
                await this.instancesResource.ResetWindowsUserAsync(
                    testInstance.InstanceReference, 
                    username);
                Assert.Fail();
            }
            catch (PasswordResetException e)
            {
                Assert.IsNotEmpty(e.Message);
            }
        }

        [Test]
        public async Task TestResetPasswordForNonexistingUserSucceeds(
            [WindowsInstance] InstanceRequest testInstance)
        {
            await testInstance.AwaitReady();

            var username = "test" + Guid.NewGuid().ToString().Substring(20);
            var credentials = await this.instancesResource.ResetWindowsUserAsync(
                testInstance.InstanceReference, 
                username);

            Assert.AreEqual(username, credentials.UserName);
            Assert.IsEmpty(credentials.Domain);
            Assert.IsNotEmpty(credentials.Password);
        }

        [Test]
        public async Task TestResetPasswordForExistingUserSucceeds(
            [WindowsInstance] InstanceRequest testInstance)
        {
            await testInstance.AwaitReady();

            var username = "existinguser";
            await this.instancesResource.ResetWindowsUserAsync(
                testInstance.InstanceReference, 
                username);
            var credentials = await this.instancesResource.ResetWindowsUserAsync(
                testInstance.InstanceReference, 
                username);

            Assert.AreEqual(username, credentials.UserName);
            Assert.IsEmpty(credentials.Domain);
            Assert.IsNotEmpty(credentials.Password);
        }
    }
}
