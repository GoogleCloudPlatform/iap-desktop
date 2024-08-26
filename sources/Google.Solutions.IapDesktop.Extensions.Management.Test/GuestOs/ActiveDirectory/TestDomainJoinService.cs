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

using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Text;
using Google.Solutions.IapDesktop.Extensions.Management.GuestOs.ActiveDirectory;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Application.ObjectModel;
using Google.Solutions.Testing.Application.Test;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using static Google.Solutions.IapDesktop.Extensions.Management.GuestOs.ActiveDirectory.DomainJoinService;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.GuestOs.ActiveDirectory
{
    [TestFixture]
    public class TestDomainJoinService : ApplicationFixtureBase
    {
        //---------------------------------------------------------------------
        // CreateStartupScript.
        //---------------------------------------------------------------------

        [Test]
        public void CreateStartupScriptContainsOperationId()
        {
            var script = DomainJoinService.CreateStartupScript(Guid.Empty);

            StringAssert.Contains(Guid.Empty.ToString(), script);
        }

        //---------------------------------------------------------------------
        // AwaitMessage.
        //---------------------------------------------------------------------
        internal class TestMessage : MessageBase
        { }

        [Test]
        public async Task WhenStreamReadReturnsNoMatch_ThenAwaitMessageKeepsPolling()
        {
            var operationId = Guid.NewGuid();

            var stream = new Mock<IAsyncReader<string>>();
            stream
                .Setup(s => s.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(string.Empty);

            stream
                .Setup(s => s.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync($"{operationId} somethingelse\ntest-message");

            stream
                .Setup(s => s.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("{}\njunk\n" + JsonConvert.SerializeObject(new TestMessage()
                {
                    OperationId = Guid.Empty.ToString(),
                    MessageType = "test-message"
                }) + "\nmorejunk\n");

            var adapter = new Mock<IComputeEngineClient>();
            adapter
                .Setup(a => a.GetSerialPortOutput(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<ushort>()))
                .Returns(stream.Object);

            var instance = new InstanceLocator("project-1", "zone-1", "instance-1");

            var operation = new Mock<IStartupScriptOperation>();
            operation.SetupGet(o => o.OperationId).Returns(Guid.Empty);
            operation.SetupGet(o => o.ComputeClient).Returns(adapter.Object);
            operation.SetupGet(o => o.Instance).Returns(instance);

            var joinAdapter = new DomainJoinService(
                new Mock<IComputeEngineClient>().AsService());

            var message = await AwaitMessageAsync<TestMessage>(
                    operation.Object,
                    "test-message",
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(Guid.Empty.ToString(), message.OperationId);
            Assert.AreEqual("test-message", message.MessageType);
        }

        //---------------------------------------------------------------------
        // JoinDomain.
        //---------------------------------------------------------------------

        [Test]
        public void WhenResetCancelled_ThenJoinDomainRestoresStartupScripts()
        {
            var adapter = new Mock<IComputeEngineClient>();
            adapter
                .Setup(a => a.ControlInstanceAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<InstanceControlCommand>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TaskCanceledException());

            var instance = new InstanceLocator("project-1", "zone-1", "instance-1");

            var operation = new Mock<IStartupScriptOperation>();
            operation.SetupGet(o => o.OperationId).Returns(Guid.Empty);
            operation.SetupGet(o => o.ComputeClient).Returns(adapter.Object);
            operation.SetupGet(o => o.Instance).Returns(instance);

            var joinAdapter = new DomainJoinService(
                new Mock<IComputeEngineClient>().AsService());

            using (var cts = new CancellationTokenSource())
            {
                ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                    () => joinAdapter.JoinDomainAsync(
                        operation.Object,
                        "domain",
                        null,
                        new System.Net.NetworkCredential(),
                        cts.Token).Wait());
            }

            operation.Verify(o => o.RestoreStartupScriptsAsync(
                It.Is<CancellationToken>(t => t == CancellationToken.None)),
                Times.Once());
        }

        [Test]
        public void WhenJoinResponseContainsError_ThenJoinDomainThrowsException()
        {
            var operationId = Guid.NewGuid();
            var instance = new InstanceLocator("project-1", "zone-1", "instance-1");

            using (var key = RSA.Create())
            {
                var stream = new Mock<IAsyncReader<string>>();
                stream
                    .Setup(s => s.ReadAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(
                        JsonConvert.SerializeObject(new DomainJoinService.HelloMessage()
                        {
                            OperationId = operationId.ToString(),
                            MessageType = "hello",
                            Exponent = Convert.ToBase64String(key.ExportParameters(false).Exponent),
                            Modulus = Convert.ToBase64String(key.ExportParameters(false).Modulus)
                        }) + "\n" +
                        JsonConvert.SerializeObject(new DomainJoinService.JoinResponse()
                        {
                            OperationId = operationId.ToString(),
                            MessageType = "join-response",
                            Succeeded = false,
                            ErrorDetails = "test"
                        }));

                var adapter = new Mock<IComputeEngineClient>();
                adapter
                    .Setup(a => a.GetSerialPortOutput(
                        It.IsAny<InstanceLocator>(),
                        It.IsAny<ushort>()))
                    .Returns(stream.Object);


                var operation = new Mock<IStartupScriptOperation>();
                operation.SetupGet(o => o.OperationId).Returns(operationId);
                operation.SetupGet(o => o.ComputeClient).Returns(adapter.Object);
                operation.SetupGet(o => o.Instance).Returns(instance);

                var joinAdapter = new DomainJoinService(
                    new Mock<IComputeEngineClient>().AsService());
                ExceptionAssert.ThrowsAggregateException<DomainJoinFailedException>(
                    () => joinAdapter.JoinDomainAsync(
                        operation.Object,
                        "example.org",
                        "instance-1",
                        new System.Net.NetworkCredential("user", "pwd", "domain"),
                        CancellationToken.None).Wait());
            }
        }
    }
}
