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
using Google.Solutions.Common.ApiExtensions.Instance;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Adapters
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class TestComputeEngineAdapter : FixtureBase
    {

        //---------------------------------------------------------------------
        // Projects.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUserInViewerRole_ThenGetProjectReturnsProject(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var adapter = new ComputeEngineAdapter(await credential);

            var project = await adapter.GetProjectAsync(
                    TestProject.ProjectId,
                    CancellationToken.None);

            Assert.IsNotNull(project);
            Assert.AreEqual(TestProject.ProjectId, project.Name);
        }

        [Test]
        public async Task WhenUserNotInRole_ThenGetProjectThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var adapter = new ComputeEngineAdapter(await credential);

            AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => adapter.GetProjectAsync(
                    TestProject.ProjectId,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenProjectIdInvalid_ThenGetProjectThrowsGoogleApiException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var adapter = new ComputeEngineAdapter(await credential);

            AssertEx.ThrowsAggregateException<GoogleApiException>(
                () => adapter.GetProjectAsync(
                    "invalid",
                    CancellationToken.None).Wait());
        }

        //---------------------------------------------------------------------
        // Instances.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUserInViewerRole_ThenListInstancesAsyncReturnsInstances(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            // Make sure there is at least one instance.
            await testInstance;
            var instanceRef = await testInstance;

            var adapter = new ComputeEngineAdapter(await credential);

            var instances = await adapter.ListInstancesAsync(
                    TestProject.ProjectId,
                    CancellationToken.None);

            Assert.Greater(instances.Count(), 1);
            Assert.IsNotNull(instances.FirstOrDefault(i => i.Name == instanceRef.Name));
        }

        [Test]
        public async Task WhenUserInViewerRole_ThenListInstancesAsyncByZoneReturnsInstances(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            // Make sure there is at least one instance.
            await testInstance;
            var instanceRef = await testInstance;

            var adapter = new ComputeEngineAdapter(await credential);

            var instances = await adapter.ListInstancesAsync(
                    new ZoneLocator(TestProject.ProjectId, instanceRef.Zone),
                    CancellationToken.None);

            Assert.Greater(instances.Count(), 1);
            Assert.IsNotNull(instances.FirstOrDefault(i => i.Name == instanceRef.Name));
        }

        [Test]
        public async Task WhenUserNotInRole_ThenListInstancesAsyncThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var adapter = new ComputeEngineAdapter(await credential);

            AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => adapter.ListInstancesAsync(
                    TestProject.ProjectId,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenUserNotInRole_ThenListInstancesAsyncByZoneThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var adapter = new ComputeEngineAdapter(await credential);

            AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => adapter.ListInstancesAsync(
                    new ZoneLocator(TestProject.ProjectId, "us-central1-a"),
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenUserNotInRole_ThenGetInstanceAsyncThrowsResourceAccessDeniedException(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;
            var adapter = new ComputeEngineAdapter(await credential);

            AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => adapter.GetInstanceAsync(
                    locator,
                    CancellationToken.None).Wait());
        }

        //---------------------------------------------------------------------
        // Nodes.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUserNotInRole_ThenListNodeGroupsAsyncThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var adapter = new ComputeEngineAdapter(await credential);

            AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => adapter.ListNodeGroupsAsync(
                    TestProject.ProjectId,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenUserNotInRole_ThenListNodesAsyncThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var adapter = new ComputeEngineAdapter(await credential);

            AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => adapter.ListNodesAsync(
                    TestProject.ProjectId,
                    CancellationToken.None).Wait());

            AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => adapter.ListNodesAsync(
                    new ZoneLocator(TestProject.ProjectId, "us-central1-a"),
                    "group-1",
                    CancellationToken.None).Wait());
        }

        //---------------------------------------------------------------------
        // Disks.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUserNotInRole_ThenListDisksAsyncThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var adapter = new ComputeEngineAdapter(await credential);

            AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => adapter.ListDisksAsync(
                    TestProject.ProjectId,
                    CancellationToken.None).Wait());
        }

        //---------------------------------------------------------------------
        // Images.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUserNotInRole_ThenGetImageThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var adapter = new ComputeEngineAdapter(await credential);

            AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => adapter.GetImageAsync(
                    new ImageLocator(TestProject.ProjectId, "someimage"),
                    CancellationToken.None).Wait());
        }

        //---------------------------------------------------------------------
        // Guest attributes.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUserNotInRole_ThenGetGuestAttributesAsyncThrowsResourceAccessDeniedException(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;
            var adapter = new ComputeEngineAdapter(await credential);

            AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => adapter.GetGuestAttributesAsync(
                    locator,
                    "somepath/",
                    CancellationToken.None).Wait());
        }

        //---------------------------------------------------------------------
        // TestPermission.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUserInRole_ThenIsGrantedPermissionReturnsTrue(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;
            var adapter = new ComputeEngineAdapter(await credential);

            var result = await adapter.IsGrantedPermission(
                locator,
                Permissions.ComputeInstancesGet);

            Assert.IsTrue(result);
        }

        [Test]
        public async Task WhenUserNotInRole_ThenIsGrantedPermissionReturnsFalse(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;
            var adapter = new ComputeEngineAdapter(await credential);

            var result = await adapter.IsGrantedPermission(
                locator,
                Permissions.ComputeInstancesSetMetadata);

            Assert.IsFalse(result);
        }

        //---------------------------------------------------------------------
        // ResetWindowsUser.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUserInRole_ThenIsGrantedPermissionToResetWindowsUserReturnsTrue(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;
            var adapter = new ComputeEngineAdapter(await credential);

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
            var adapter = new ComputeEngineAdapter(await credential);

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
            var adapter = new ComputeEngineAdapter(await credential);
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
            var adapter = new ComputeEngineAdapter(await credential);
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
            var adapter = new ComputeEngineAdapter(await credential);
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
            var adapter = new ComputeEngineAdapter(await credential);
            var username = "test" + Guid.NewGuid().ToString().Substring(20);

            var result = await adapter.ResetWindowsUserAsync(
                await testInstance,
                username,
                CancellationToken.None);
            Assert.IsNotNull(result.Password);
        }

        //---------------------------------------------------------------------
        // Proxy.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenProxyEnabledAndCredentialsCorrect_ThenRequestSucceeds(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var proxyCredentials = new NetworkCredential("proxyuser", "proxypass");
            using (var proxy = new InProcessAuthenticatingHttpProxy(
                proxyCredentials))
            {
                var proxyAdapter = new HttpProxyAdapter();
                proxyAdapter.UseCustomProxySettings(
                    new Uri($"http://localhost:{proxy.Port}"),
                    proxyCredentials);

                using (var adapter = new ComputeEngineAdapter(await credential))
                {
                    await adapter.GetProjectAsync(TestProject.ProjectId, CancellationToken.None);
                }
            }
        }

        [Test]
        public async Task WhenProxyEnabledAndCredentialsWrong_ThenRequestThrowsWebException(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var proxyCredentials = new NetworkCredential("proxyuser", "proxypass");
            using (var proxy = new InProcessAuthenticatingHttpProxy(
                proxyCredentials))
            {
                var proxyAdapter = new HttpProxyAdapter();
                proxyAdapter.UseCustomProxySettings(
                    new Uri($"http://localhost:{proxy.Port}"),
                    new NetworkCredential("proxyuser", "wrong"));

                try
                {
                    using (var adapter = new ComputeEngineAdapter(await credential))
                    {
                        await adapter.GetProjectAsync(TestProject.ProjectId, CancellationToken.None);
                        Assert.Fail("Exception expected");
                    }
                }
                catch (HttpRequestException e) when (e.InnerException is WebException exception)
                {
                    Assert.AreEqual(
                        HttpStatusCode.ProxyAuthenticationRequired,
                        ((HttpWebResponse)exception.Response).StatusCode);
                }
            }
        }
    }
}
