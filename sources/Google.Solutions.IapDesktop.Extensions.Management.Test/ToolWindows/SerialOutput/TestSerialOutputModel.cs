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

using Google.Apis.Auth.OAuth2.Responses;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Text;
using Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.SerialOutput;
using Google.Solutions.Testing.Apis.Integration;
using Google.Solutions.Testing.Application.Test;
using Moq;
using NUnit.Framework;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.ToolWindows.SerialOutput
{
    [TestFixture]
    [UsesCloudResources]
    public class TestSerialOutputModel : ApplicationFixtureBase
    {
        public const ushort ConsolePort = 1;

        [Test]
        public async Task Load(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            await testInstance;

            var computeClient = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var model = await SerialOutputModel
                .LoadAsync(
                    "display-name",
                    computeClient,
                    await testInstance,
                    ConsolePort,
                    CancellationToken.None)
                .ConfigureAwait(true);

            Assert.IsFalse(string.IsNullOrWhiteSpace(model.Output));
            Assert.That(model.DisplayName, Is.EqualTo("display-name"));
            Assert.That(model.Output, Does.Contain("Finished running startup scripts"));
        }

        //---------------------------------------------------------------------
        // Tail.
        //---------------------------------------------------------------------

        [Test]
        public async Task Tail_WhenCancelled()
        {
            var stream = new Mock<IAsyncReader<string>>();
            stream.Setup(s => s.ReadAsync(
                It.IsAny<CancellationToken>()))
                .ReturnsAsync("");

            var adapter = new Mock<IComputeEngineClient>();
            adapter.Setup(a => a.GetSerialPortOutput(
                It.IsAny<InstanceLocator>(),
                1)).Returns(stream.Object);

            // Let it load successfully...
            var model = await SerialOutputModel
                .LoadAsync(
                    "display-name",
                    adapter.Object,
                    new InstanceLocator("project-1", "zone-1", "instance-1"),
                    ConsolePort,
                    CancellationToken.None)
                .ConfigureAwait(true);

            using (var cts = new CancellationTokenSource())
            {
                var tailTask = model.TailAsync(
                    _ => { },
                    cts.Token);

                cts.Cancel();

                // Now the task should finish quickly.
                await tailTask.ConfigureAwait(true);
            }
        }

        [Test]
        public async Task Tail_WhenApiThrowsException_ThenMessageIsTailedToOutput()
        {
            var stream = new Mock<IAsyncReader<string>>();
            stream.Setup(s => s.ReadAsync(
                It.IsAny<CancellationToken>()))
                .ReturnsAsync("");

            var adapter = new Mock<IComputeEngineClient>();
            adapter.Setup(a => a.GetSerialPortOutput(
                It.IsAny<InstanceLocator>(),
                1)).Returns(stream.Object);

            // Let it load successfully...
            var model = await SerialOutputModel
                .LoadAsync(
                    "display-name",
                    adapter.Object,
                    new InstanceLocator("project-1", "zone-1", "instance-1"),
                    ConsolePort,
                    CancellationToken.None)
                .ConfigureAwait(true);

            // ...but fail the tailing.
            stream.Setup(s => s.ReadAsync(
                It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<string>(
                    new TokenResponseException(new TokenErrorResponse())));

            using (var cts = new CancellationTokenSource())
            {
                var newOutput = new StringBuilder();

                var tailTask = model.TailAsync(
                    s => newOutput.Append(s),
                    cts.Token);

                // The exception should cause the task should finish.
                await tailTask.ConfigureAwait(true);

                Assert.That(newOutput.ToString(), Does.Contain("session timed out"));
                Assert.That(model.Output, Does.Not.Contain("session timed out"));
            }
        }
    }
}
