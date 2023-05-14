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

using Google.Solutions.Iap.Protocol;
using Google.Solutions.IapDesktop.Core.Net.Transport;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Test.Net.Transport
{
    [TestFixture]
    public class TestIapTunnel
    {
        private static readonly IPEndPoint LoopbackEndpoint
            = new IPEndPoint(IPAddress.Loopback, 8000);

        //---------------------------------------------------------------------
        // Properties.
        //---------------------------------------------------------------------

        [Test]
        public void Statistics()
        {
            var listener = new Mock<ISshRelayListener>();
            listener.SetupGet(l => l.LocalPort).Returns(LoopbackEndpoint.Port);
            listener.SetupGet(l => l.Statistics).Returns(new Iap.Net.ConnectionStatistics());

            using (var tunnel = new IapTunnel(
                listener.Object,
                LoopbackEndpoint,
                IapTunnelFlags.None))
            {
                Assert.AreEqual(0, tunnel.Statistics.BytesReceived);
                Assert.AreEqual(0, tunnel.Statistics.BytesTransmitted);
            }
        }

        [Test]
        public void LocalEndpoint()
        {
            var listener = new Mock<ISshRelayListener>();
            listener.SetupGet(l => l.LocalPort).Returns(LoopbackEndpoint.Port);

            using (var tunnel = new IapTunnel(
                listener.Object,
                LoopbackEndpoint,
                IapTunnelFlags.None))
            {
                Assert.AreEqual(LoopbackEndpoint, tunnel.LocalEndpoint);
            }
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        [Test]
        public void DisposeStopsRelay()
        {
            CancellationToken token;
            var listener = new Mock<ISshRelayListener>();
            listener.SetupGet(l => l.LocalPort).Returns(LoopbackEndpoint.Port);
            listener
                .Setup(l => l.ListenAsync(It.IsAny<CancellationToken>()))
                .Callback((CancellationToken t) => token = t)
                .Returns(Task.CompletedTask);

            using (new IapTunnel(
                listener.Object,
                LoopbackEndpoint,
                IapTunnelFlags.None))
            {
                Assert.IsFalse(token.IsCancellationRequested);
            }

            Assert.IsTrue(token.IsCancellationRequested);
        }

        //---------------------------------------------------------------------
        // Close.
        //---------------------------------------------------------------------

        [Test]
        public void CloseStopsRelay()
        {
            Task listenTask = new TaskCompletionSource<object>().Task;
            CancellationToken token;

            var listener = new Mock<ISshRelayListener>();
            listener.SetupGet(l => l.LocalPort).Returns(LoopbackEndpoint.Port);
            listener
                .Setup(l => l.ListenAsync(It.IsAny<CancellationToken>()))
                .Callback((CancellationToken t) => token = t)
                .Returns(listenTask);

            using (var tunnel = new IapTunnel(
                listener.Object,
                LoopbackEndpoint,
                IapTunnelFlags.None))
            {
                Assert.IsFalse(token.IsCancellationRequested);
                Assert.AreSame(listenTask, tunnel.CloseAsync());
                Assert.IsTrue(token.IsCancellationRequested);
            }
        }
    }
}
