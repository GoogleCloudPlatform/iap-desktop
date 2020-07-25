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
using Google.Solutions.Common.ApiExtensions.Instance;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.Common.Text;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.SerialOutput;
using Moq;
using NUnit.Framework;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.Services.SerialOutput
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class TestSerialOutputModel : FixtureBase
    {
        [Test]
        public async Task WhenLoadAsyncCompletes_ThenOutputContainsExistingData(
            [WindowsInstance] InstanceRequest testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] CredentialRequest credential)
        {
            await testInstance.AwaitReady();

            var model = await SerialOutputModel.LoadAsync(
                "display-name",
                new ComputeEngineAdapter(await credential.GetCredentialAsync()),
                await testInstance.GetInstanceAsync(),
                SerialPortStream.ConsolePort,
                CancellationToken.None);

            Assert.IsFalse(string.IsNullOrWhiteSpace(model.Output));
            Assert.AreEqual("display-name", model.DisplayName);
            StringAssert.Contains("Finished running startup scripts", model.Output);
        }

        [Test]
        public async Task WhenTailing_ThenCancelStopsTask()
        {
            var stream = new Mock<IAsyncReader<string>>();
            stream.Setup(s => s.ReadAsync(
                It.IsAny<CancellationToken>()))
                .ReturnsAsync("");

            var adapter = new Mock<IComputeEngineAdapter>();
            adapter.Setup(a => a.GetSerialPortOutput(
                It.IsAny<InstanceLocator>(),
                1)).Returns(stream.Object);

            // Let it load successfully...
            var model = await SerialOutputModel.LoadAsync(
                "display-name",
                adapter.Object,
                new InstanceLocator("project-1", "zone-1", "instance-1"),
                SerialPortStream.ConsolePort,
                CancellationToken.None);

            using (var cts = new CancellationTokenSource())
            {
                var tailTask = model.TailAsync(
                    _ => { },
                    cts.Token);

                cts.Cancel();

                // Now the task should finish quickly.
                await tailTask;
            }
        }

        [Test]
        public async Task WhenApiThrowsException_ThenMessageIsTailedToOutput()
        {
            var stream = new Mock<IAsyncReader<string>>();
            stream.Setup(s => s.ReadAsync(
                It.IsAny<CancellationToken>()))
                .ReturnsAsync("");

            var adapter = new Mock<IComputeEngineAdapter>();
            adapter.Setup(a => a.GetSerialPortOutput(
                It.IsAny<InstanceLocator>(),
                1)).Returns(stream.Object);

            // Let it load successfully...
            var model = await SerialOutputModel.LoadAsync(
                "display-name",
                adapter.Object,
                new InstanceLocator("project-1", "zone-1", "instance-1"),
                SerialPortStream.ConsolePort,
                CancellationToken.None);

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
                await tailTask;

                StringAssert.Contains("session timed out", newOutput.ToString());
                StringAssert.DoesNotContain("session timed out", model.Output);
            }
        }
    }
}
