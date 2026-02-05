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
        public async Task GetProject_WhenPscEnabled(
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
            Assert.That(project.Name, Is.EqualTo(TestProject.ProjectId));
        }

        //---------------------------------------------------------------------
        // Projects.
        //---------------------------------------------------------------------

        [Test]
        public async Task GetProject_WhenUserInViewerRole(
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
            Assert.That(project.Name, Is.EqualTo(TestProject.ProjectId));
        }

        [Test]
        public async Task GetProject_WhenUserNotInRole(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            await ExceptionAssert
                .ThrowsAsync<ResourceAccessDeniedException>(() => client.GetProjectAsync(
                    new ProjectLocator(TestProject.ProjectId),
                    CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task GetProject_WhenProjectIdInvalid_ThenThrowsException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            await ExceptionAssert
                .ThrowsAsync<ResourceNotFoundException>(() => client.GetProjectAsync(
                    new ProjectLocator(TestProject.InvalidProjectId),
                    CancellationToken.None))
                .ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // Instances.
        //---------------------------------------------------------------------

        [Test]
        public async Task ListInstancesByProject_WhenUserInViewerRole_(
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
        public async Task ListInstancesByZone_WhenUserInViewerRole(
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
        public async Task ListInstancesByProject_WhenUserNotInRole(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            await ExceptionAssert
                .ThrowsAsync<ResourceAccessDeniedException>(() => client.ListInstancesAsync(
                    new ProjectLocator(TestProject.ProjectId),
                    CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ListInstancesByZone_WhenUserNotInRole(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            await ExceptionAssert
                .ThrowsAsync<ResourceAccessDeniedException>(() => client.ListInstancesAsync(
                    new ZoneLocator(TestProject.ProjectId, "us-central1-a"),
                    CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task GetInstance_WhenUserNotInRole(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var locator = await testInstance;
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            await ExceptionAssert
                .ThrowsAsync<ResourceAccessDeniedException>(() => client.GetInstanceAsync(
                    locator,
                    CancellationToken.None))
                .ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // Guest attributes.
        //---------------------------------------------------------------------

        [Test]
        public async Task GetGuestAttributes_WhenUserNotInRole(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var locator = await testInstance;
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            await ExceptionAssert
                .ThrowsAsync<ResourceAccessDeniedException>(() => client.GetGuestAttributesAsync(
                    locator,
                    "somepath/",
                    CancellationToken.None))
                .ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // Serial port.
        //---------------------------------------------------------------------

        [Test]
        public async Task GetSerialPortOutput_WhenLaunchingInstance_ThenInstanceSetupFinishedTextAppearsInStream(
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
        public async Task ControlInstance_WhenInstanceNotFound(
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            await ExceptionAssert
                .ThrowsAsync<ResourceNotFoundException>(() => client.ControlInstanceAsync(
                    new InstanceLocator(TestProject.ProjectId, "us-central1-a", "doesnotexist"),
                    InstanceControlCommand.Start,
                    CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ControlInstance_Start_WhenInstanceRunning(
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
        public async Task ControlInstance_WhenUserNotInRole(
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

            await ExceptionAssert
                .ThrowsAsync<ResourceAccessDeniedException>(() => client.ControlInstanceAsync(
                    instance,
                    command,
                    CancellationToken.None))
                .ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // Permission check.
        //---------------------------------------------------------------------

        [Test]
        public async Task IsAccessGranted_WhenUserInRole_ThenReturnsTrue(
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
        public async Task IsAccessGranted_WhenUserNotInRole_ThenReturnsFalse(
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
        public async Task IsAccessGranted_WhenUserLacksInstanceListPermission_ThenFailsOpenAndReturnsTrue(
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
