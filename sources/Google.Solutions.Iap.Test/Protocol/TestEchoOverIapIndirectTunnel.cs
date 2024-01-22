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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Iap.Net;
using Google.Solutions.Iap.Protocol;
using Google.Solutions.Testing.Apis.Integration;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Iap.Test.Protocol
{
    [TestFixture]
    [UsesCloudResources]
    public class TestEchoOverIapIndirectTunnel : TestEchoOverIapBase
    {
        protected override INetworkStream ConnectToEchoServer(
            InstanceLocator vmRef,
            IAuthorization authorization)
        {
            var policy = new Mock<IIapListenerPolicy>();
            policy.Setup(p => p.IsClientAllowed(It.IsAny<IPEndPoint>())).Returns(true);

            var client = new IapClient(
                IapClient.CreateEndpoint(),
                authorization,
                TestProject.UserAgent);

            var listener = new IapListener(
                client.GetTarget(vmRef, 7, IapClient.DefaultNetworkInterface),
                policy.Object,
                null)
            {
                ClientAcceptLimit = 1 // Terminate after first connection.
            };

            listener.ListenAsync(CancellationToken.None);

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(listener.LocalEndpoint);

            return new SocketStream(socket, new NetworkStatistics());
        }

        [Test]
        public async Task WhenSendingMessagesToEchoServer_MessagesAreReceivedVerbatim(
            [LinuxInstance(InitializeScript = InitializeScripts.InstallEchoServer)] ResourceTask<InstanceLocator> vm,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth,
            [Values(
                1,
                (int)SshRelayFormat.Data.MaxPayloadLength - 1,
                (int)SshRelayFormat.Data.MaxPayloadLength,
                (int)SshRelayFormat.Data.MaxPayloadLength + 1,
                (int)SshRelayFormat.Data.MaxPayloadLength * 2)] int messageSize,
            [Values(1, 3)] int count)
        {
            await WhenSendingMessagesToEchoServer_MessagesAreReceivedVerbatim(
                    await vm,
                    await auth,
                    messageSize,
                    messageSize,
                    messageSize,
                    count)
                .ConfigureAwait(false);
        }
    }
}
