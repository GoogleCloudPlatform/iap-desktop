//
// Copyright 2023 Google LLC
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
using Google.Solutions.Apis.Logging;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Test.Logging
{
    [TestFixture]
    [UsesCloudResources]
    public class TestLoggingClient
    {
        //---------------------------------------------------------------------
        // ReadLogs.
        //---------------------------------------------------------------------

        [Test]
        public async Task ReadLogs_WhenUserNotInRole_ThenReadLogsThrowsException(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var adapter = new LoggingClient(
                LoggingClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            await ExceptionAssert
                .ThrowsAsync<ResourceAccessDeniedException>(() => adapter.ReadLogsAsync(
                    new[] { $"projects/{TestProject.ProjectId}" },
                    string.Empty,
                    _ => null,
                    CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ReadLogs_WhenProjectIdInvalid_ThenReadLogsThrowsException(
            [Credential(Role = PredefinedRole.LogsViewer)] ResourceTask<IAuthorization> auth)
        {
            var adapter = new LoggingClient(
                LoggingClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            await ExceptionAssert
                .ThrowsAsync<GoogleApiException>(() => adapter.ReadLogsAsync(
                    new[] { $"projects/{TestProject.InvalidProjectId}" },
                    string.Empty,
                    _ => null,
                    CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ReadLogs_WhenUserInViewerRole_ThenReadLogsInvokesCallback(
            [Credential(Role = PredefinedRole.LogsViewer)] ResourceTask<IAuthorization> auth)
        {
            var adapter = new LoggingClient(
                LoggingClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var callbackInvoked = false;
            await adapter
                .ReadLogsAsync(
                    new[] { $"projects/{TestProject.ProjectId}" },
                    string.Empty,
                    stream =>
                    {
                        Assert.That(stream, Is.Not.Null);
                        callbackInvoked = true;
                        return null;
                    },
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(callbackInvoked, Is.True);
        }
    }
}
