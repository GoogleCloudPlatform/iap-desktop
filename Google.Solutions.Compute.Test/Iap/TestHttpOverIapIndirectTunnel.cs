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

using Google.Apis.Auth.OAuth2;
using Google.Solutions.Compute.Iap;
using Google.Solutions.Compute.Net;
using Google.Solutions.Compute.Test.Env;
using NUnit.Framework;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Compute.Test.Iap
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class TestHttpOverIapIndirectTunnel : TestHttpOverIapTunnelBase
    {
        protected override INetworkStream ConnectToWebServer(VmInstanceReference vmRef)
        {
            var listener = SshRelayListener.CreateLocalListener(
                new IapTunnelingEndpoint(
                    GoogleCredential.GetApplicationDefault(),
                    vmRef,
                    80,
                    IapTunnelingEndpoint.DefaultNetworkInterface));
            listener.ClientAcceptLimit = 1; // Terminate after first connection.
            listener.ListenAsync(CancellationToken.None);

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(IPAddress.Loopback, listener.LocalPort));

            return new SocketStream(socket);
        }


        [Test]
        public async Task ServerNotListeningCausesZeroRead(
            [LinuxInstance] InstanceRequest vm)
        {
            await vm.AwaitReady();
            var stream = ConnectToWebServer(vm.InstanceReference);

            byte[] request = new ASCIIEncoding().GetBytes(
                    $"GET / HTTP/1.1\r\nHost:www\r\nConnection: keep-alive\r\n\r\n");
            await stream.WriteAsync(request, 0, request.Length, CancellationToken.None);

            byte[] buffer = new byte[stream.MinReadSize];
            Assert.AreEqual(0, stream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None).Result);
        }
    }
}
