﻿//
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
using Google.Solutions.Apis.Locator;
using Google.Solutions.Iap.Protocol;
using Google.Solutions.Iap.Net;
using Google.Solutions.Testing.Common.Integration;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Google.Solutions.Iap.Test.Protocol
{
    [TestFixture]
    [UsesCloudResources]
    public class TestEchoOverIapDirectTunnel : TestEchoOverIapBase
    {
        protected override INetworkStream ConnectToEchoServer(
            InstanceLocator vmRef,
            ICredential credential)
        {
            return new SshRelayStream(
                new IapTunnelingEndpoint(
                    credential,
                    vmRef,
                    7,
                    IapTunnelingEndpoint.DefaultNetworkInterface,
                    TestProject.UserAgent));
        }

        [Test]
        public async Task WhenSendingMessagesToEchoServer_MessagesAreReceivedVerbatim(
            [LinuxInstance(InitializeScript = InitializeScripts.InstallEchoServer)] ResourceTask<InstanceLocator> vm,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential,
            [Values(
                1,
                (int)SshRelayFormat.Data.MaxPayloadLength - 1,
                (int)SshRelayFormat.Data.MaxPayloadLength,
                (int)SshRelayFormat.Data.MaxPayloadLength + 1,
                (int)SshRelayFormat.Data.MaxPayloadLength * 2)] int messageSize,
            [Values(
                SshRelayStream.MaxWriteSize / 2,
                SshRelayStream.MaxWriteSize)] int writeSize,
            [Values(
                SshRelayStream.MinReadSize,
                SshRelayStream.MinReadSize * 2)] int readSize,
            [Values(1, 3)] int count)
        {
            await WhenSendingMessagesToEchoServer_MessagesAreReceivedVerbatim(
                    await vm,
                    await credential,
                    messageSize,
                    writeSize,
                    readSize,
                    count)
                .ConfigureAwait(false);
        }
    }
}
