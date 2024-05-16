﻿//
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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Test.Compute
{
    [TestFixture]
    [UsesCloudResources]
    public class TestComputeEngineClient
    {
        //---------------------------------------------------------------------
        // PSC.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenPscEnabled_ThenRequestSucceeds(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var address = await Dns
                .GetHostAddressesAsync(ComputeEngineClient.CreateEndpoint().CanonicalUri.Host)
                .ConfigureAwait(false);

            //
            // Use IP address as pseudo-PSC endpoint.
            //
            var endpoint = ComputeEngineClient.CreateEndpoint(
                new ServiceRoute(address.FirstOrDefault().ToString()));

            var client = new ComputeEngineClient(
                endpoint,
                await auth,
                TestProject.UserAgent);

            var project = await client
                .GetProjectAsync(
                    new ProjectLocator(TestProject.ProjectId),
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(project);
            Assert.AreEqual(TestProject.ProjectId, project.Name);
        }

        //---------------------------------------------------------------------
        // Projects.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUserInViewerRole_ThenGetProjectReturnsProject(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var project = await client
                .GetProjectAsync(
                    new ProjectLocator(TestProject.ProjectId),
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(project);
            Assert.AreEqual(TestProject.ProjectId, project.Name);
        }

        [Test]
        public async Task WhenUserNotInRole_ThenGetProjectThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            ExceptionAssert.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => client.GetProjectAsync(
                    new ProjectLocator(TestProject.ProjectId),
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenProjectIdInvalid_ThenGetProjectThrowsException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            ExceptionAssert.ThrowsAggregateException<ResourceNotFoundException>(
                () => client.GetProjectAsync(
                    new ProjectLocator(TestProject.InvalidProjectId),
                    CancellationToken.None).Wait());
        }

        //---------------------------------------------------------------------
        // Instances.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUserInViewerRole_ThenListInstancesAsyncReturnsInstances(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            // Make sure there is at least one instance.
            await testInstance;
            var instanceRef = await testInstance;

            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var instances = await client
                .ListInstancesAsync(
                    new ProjectLocator(TestProject.ProjectId),
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.Greater(instances.Count(), 1);
            Assert.IsNotNull(instances.FirstOrDefault(i => i.Name == instanceRef.Name));
        }

        [Test]
        public async Task WhenUserInViewerRole_ThenListInstancesAsyncByZoneReturnsInstances(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            // Make sure there is at least one instance.
            await testInstance;
            var instanceRef = await testInstance;

            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var instances = await client.ListInstancesAsync(
                    new ZoneLocator(TestProject.ProjectId, instanceRef.Zone),
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.Greater(instances.Count(), 1);
            Assert.IsNotNull(instances.FirstOrDefault(i => i.Name == instanceRef.Name));
        }

        [Test]
        public async Task WhenUserNotInRole_ThenListInstancesAsyncThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            ExceptionAssert.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => client.ListInstancesAsync(
                    new ProjectLocator(TestProject.ProjectId),
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenUserNotInRole_ThenListInstancesAsyncByZoneThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            ExceptionAssert.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => client.ListInstancesAsync(
                    new ZoneLocator(TestProject.ProjectId, "us-central1-a"),
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenUserNotInRole_ThenGetInstanceAsyncThrowsResourceAccessDeniedException(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var locator = await testInstance;
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            ExceptionAssert.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => client.GetInstanceAsync(
                    locator,
                    CancellationToken.None).Wait());
        }

        //---------------------------------------------------------------------
        // Guest attributes.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUserNotInRole_ThenGetGuestAttributesAsyncThrowsResourceAccessDeniedException(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var locator = await testInstance;
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            ExceptionAssert.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => client.GetGuestAttributesAsync(
                    locator,
                    "somepath/",
                    CancellationToken.None).Wait());
        }

        //---------------------------------------------------------------------
        // Serial port.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenLaunchingInstance_ThenInstanceSetupFinishedTextAppearsInStream(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var stream = client.GetSerialPortOutput(
                await testInstance,
                1);

            var startTime = DateTime.Now;

            while (DateTime.Now < startTime.AddMinutes(3))
            {
                var log = await stream
                    .ReadAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                if (log.Contains("Finished running startup scripts"))
                {
                    return;
                }
            }

            Assert.Fail("Timeout waiting for serial console output to appear");
        }

        //---------------------------------------------------------------------
        // Control instance lifecycle.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenInstanceNotFound_ThenControlInstanceThrowsException(
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            ExceptionAssert.ThrowsAggregateException<ResourceNotFoundException>(
                () => client.ControlInstanceAsync(
                    new InstanceLocator(TestProject.ProjectId, "us-central1-a", "doesnotexist"),
                    InstanceControlCommand.Start,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenInstanceRunning_ThenStartingWithControlInstanceSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            await client.ControlInstanceAsync(
                    await testInstance,
                    InstanceControlCommand.Start,
                    CancellationToken.None)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenUserInRole_ThenControlInstanceFails(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth,
            [Values(
                InstanceControlCommand.Stop,
                InstanceControlCommand.Resume,
                InstanceControlCommand.Reset,
                InstanceControlCommand.Suspend)] InstanceControlCommand command)
        {
            var instance = await testInstance;
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            ExceptionAssert.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => client.ControlInstanceAsync(
                    instance,
                    command,
                    CancellationToken.None).Wait());
        }

        //---------------------------------------------------------------------
        // Permission check.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUserInRole_ThenIsAccessGrantedReturnsTrue(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var locator = await testInstance;
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var result = await client.IsAccessGrantedAsync(
                    locator,
                    Permissions.ComputeInstancesGet)
                .ConfigureAwait(false);

            Assert.IsTrue(result);
        }

        [Test]
        public async Task WhenUserNotInRole_ThenIsAccessGrantedReturnsFalse(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var locator = await testInstance;
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var result = await client.IsAccessGrantedAsync(
                    locator,
                    Permissions.ComputeInstancesSetMetadata)
                .ConfigureAwait(false);

            Assert.IsFalse(result);
        }

        [Test]
        public async Task WhenUserLacksInstanceListPermission_ThenIsAccessGrantedFailsOpenAndReturnsTrue(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var locator = await testInstance;
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var result = await client.IsAccessGrantedAsync(
                    locator,
                    Permissions.ComputeInstancesSetMetadata)
                .ConfigureAwait(false);

            Assert.IsTrue(result);
        }
    }
}
