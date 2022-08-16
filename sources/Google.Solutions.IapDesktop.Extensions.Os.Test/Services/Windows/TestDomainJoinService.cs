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
using Google.Solutions.Common.Locator;
using Google.Solutions.Testing.Common.Integration;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Os.Services.Inventory;
using NUnit.Framework;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Solutions.Testing.Common;
using Google.Solutions.Testing.Application.Test;
using Google.Solutions.IapDesktop.Extensions.Os.Services.Windows;
using Moq;
using System;
using System.Collections.Generic;
using Google.Apis.Compute.v1.Data;
using Google.Solutions.Common.Text;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace Google.Solutions.IapDesktop.Extensions.Os.Test.Services.Windows
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
            var operationId = Guid.NewGuid();
            var script = DomainJoinService.CreateStartupScript(operationId);

            StringAssert.Contains(operationId.ToString(), script);
        }

        //---------------------------------------------------------------------
        // ReplaceMetadataItems.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenMetadataKeyDoNotExist_ThenReplaceMetadataItemsReturnsEmptyList()
        {
            var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
            computeEngineAdapter.Setup(a => a.UpdateMetadataAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()))
                .Callback((InstanceLocator i, Action<Metadata> action, CancellationToken t) =>
                {
                    action(new Metadata());
                });

            var instance = new InstanceLocator("project-1", "zone-1", "instance-1");
            using (var joinAdapter = new DomainJoinService(computeEngineAdapter.Object))
            {
                var oldItems = await joinAdapter.ReplaceMetadataItemsAsync(
                        instance,
                        null,
                        new[] { "old-1", "old-2" },
                        new List<Metadata.ItemsData>()
                        {
                        new Metadata.ItemsData()
                        {
                            Key = "new-1"
                        }
                        },
                        CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(oldItems);
                CollectionAssert.IsEmpty(oldItems);
            }
        }

        [Test]
        public async Task WhenMetadataKeysExist_ThenReplaceMetadataItemsReturnsList()
        {
            var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
            computeEngineAdapter.Setup(a => a.UpdateMetadataAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()))
                .Callback((InstanceLocator i, Action<Metadata> action, CancellationToken t) =>
                {
                    action(new Metadata()
                    {
                        Items = new List<Metadata.ItemsData>()
                        {
                            new Metadata.ItemsData() { Key = "old-1" },
                            new Metadata.ItemsData() { Key = "old-2" },
                            new Metadata.ItemsData() { Key = "old-3" }
                        }
                    });
                });


            var instance = new InstanceLocator("project-1", "zone-1", "instance-1");
            using (var joinAdapter = new DomainJoinService(computeEngineAdapter.Object))
            {
                var oldItems = await joinAdapter.ReplaceMetadataItemsAsync(
                        instance,
                        null,
                        new[] { "old-1", "old-2" },
                        new List<Metadata.ItemsData>()
                        {
                        new Metadata.ItemsData()
                        {
                            Key = "new-1"
                        }
                        },
                        CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(oldItems);

                CollectionAssert.AreEquivalent(
                    new[] { "old-1", "old-2" },
                    oldItems.Select(i => i.Key).ToList());
            }
        }

        [Test]
        public void WhenMetadataContainsGuardKey_ThenReplaceMetadataItemsThrowsException()
        {
            var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
            computeEngineAdapter.Setup(a => a.UpdateMetadataAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()))
                .Callback((InstanceLocator i, Action<Metadata> action, CancellationToken t) =>
                {
                    action(new Metadata()
                    {
                        Items = new List<Metadata.ItemsData>()
                        {
                            new Metadata.ItemsData()
                            {
                                Key = "guard"
                            }
                        }
                    });
                });

            var instance = new InstanceLocator("project-1", "zone-1", "instance-1");
            using (var joinAdapter = new DomainJoinService(computeEngineAdapter.Object))
            {
                ExceptionAssert.ThrowsAggregateException<InvalidOperationException>(
                    () => joinAdapter.ReplaceMetadataItemsAsync(
                        instance,
                        "guard",
                        new[] { "old-1", "old-2" },
                        new List<Metadata.ItemsData>()
                        {
                        new Metadata.ItemsData()
                        {
                            Key = "new-1"
                        }
                        },
                        CancellationToken.None).Wait());
            }
        }

        //---------------------------------------------------------------------
        // AwaitMessage.
        //---------------------------------------------------------------------

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
                .ReturnsAsync($"{operationId} test-message");

            var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
            computeEngineAdapter
                .Setup(a => a.GetSerialPortOutput(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<ushort>()))
                .Returns(stream.Object);

            using (var joinAdapter = new DomainJoinService(computeEngineAdapter.Object))
            {
                var instance = new InstanceLocator("project-1", "zone-1", "instance-1");
                var match = await joinAdapter.AwaitMessageAsync(
                        instance,
                        operationId,
                        "test-message",
                        CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual($"{operationId} test-message", match);
            }
        }

        //---------------------------------------------------------------------
        // JoinDomain.
        //---------------------------------------------------------------------

        [Test]
        public void WhenJoinResponseContainsError_ThenJoinDomainThrowsException()
        {
            var operationId = Guid.NewGuid();

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

                var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
                computeEngineAdapter
                    .Setup(a => a.GetSerialPortOutput(
                        It.IsAny<InstanceLocator>(),
                        It.IsAny<ushort>()))
                    .Returns(stream.Object);
                computeEngineAdapter.Setup(a => a.UpdateMetadataAsync(
                        It.IsAny<InstanceLocator>(),
                        It.IsAny<Action<Metadata>>(),
                        It.IsAny<CancellationToken>()))
                    .Callback((InstanceLocator i, Action<Metadata> action, CancellationToken t) =>
                    {
                        action(new Metadata());
                    });

                using (var joinAdapter = new DomainJoinService(computeEngineAdapter.Object))
                {
                    var instance = new InstanceLocator("project-1", "zone-1", "instance-1");

                    ExceptionAssert.ThrowsAggregateException<DomainJoinFailedException>(
                        () => joinAdapter.JoinDomainAsync(
                            instance,
                            "example.org",
                            "instance-1",
                            new System.Net.NetworkCredential("user", "pwd", "domain"),
                            operationId,
                            CancellationToken.None).Wait());

                    computeEngineAdapter.Verify(
                        a => a.UpdateMetadataAsync(
                            It.IsAny<InstanceLocator>(),
                            It.IsAny<Action<Metadata>>(),
                            It.IsAny<CancellationToken>()),
                        Times.Exactly(3));
                }
            }
        }

        [Test]
        public void WhenJoinCancelled_ThenJoinDomainRestoresMetadata()
        {
            var operationId = Guid.NewGuid();

            using (var key = RSA.Create())
            {
                var stream = new Mock<IAsyncReader<string>>();
                stream
                    .Setup(s => s.ReadAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new TaskCanceledException());

                var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
                computeEngineAdapter
                    .Setup(a => a.GetSerialPortOutput(
                        It.IsAny<InstanceLocator>(),
                        It.IsAny<ushort>()))
                    .Returns(stream.Object);
                computeEngineAdapter.Setup(a => a.UpdateMetadataAsync(
                        It.IsAny<InstanceLocator>(),
                        It.IsAny<Action<Metadata>>(),
                        It.IsAny<CancellationToken>()))
                    .Callback((InstanceLocator i, Action<Metadata> action, CancellationToken t) =>
                    {
                        action(new Metadata());
                    });

                using (var cts = new CancellationTokenSource())
                using (var joinAdapter = new DomainJoinService(computeEngineAdapter.Object))
                {
                    var instance = new InstanceLocator("project-1", "zone-1", "instance-1");

                    ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                        () => joinAdapter.JoinDomainAsync(
                            instance,
                            "example.org",
                            "instance-1",
                            new System.Net.NetworkCredential("user", "pwd", "domain"),
                            operationId,
                            cts.Token).Wait());

                    computeEngineAdapter.Verify(
                        a => a.UpdateMetadataAsync(
                            It.IsAny<InstanceLocator>(),
                            It.IsAny<Action<Metadata>>(),
                            It.Is<CancellationToken>(t => t == CancellationToken.None)),
                        Times.Once);
                    computeEngineAdapter.Verify(
                        a => a.UpdateMetadataAsync(
                            It.IsAny<InstanceLocator>(),
                            It.IsAny<Action<Metadata>>(),
                            It.Is<CancellationToken>(t => t != CancellationToken.None)),
                        Times.Once);
                }
            }
        }
    }
}
