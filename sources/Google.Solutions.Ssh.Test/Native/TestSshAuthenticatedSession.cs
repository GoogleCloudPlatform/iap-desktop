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

using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.Ssh.Native;
using NUnit.Framework;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test.Native
{
    [TestFixture]
    public class TestSshAuthenticatedSession : SshFixtureBase
    {
        //---------------------------------------------------------------------
        // Channel.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenDisconnected_ThenOpenExecChannelAsyncThrowsSocketSend(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instanceLocator = await instanceLocatorTask;
            var endpoint = new IPEndPoint(
                await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                22);
            using (var key = new RsaSshKey(new RSACng()))
            {
                await InstanceUtil.AddPublicKeyToMetadata(
                    instanceLocator,
                    "testuser",
                    key).ConfigureAwait(true);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                using (var authSession = connection.Authenticate("testuser", key))
                {
                    connection.Dispose();
                    SshAssert.ThrowsNativeExceptionWithError(
                        session,
                        LIBSSH2_ERROR.SOCKET_SEND,
                        () => authSession.OpenExecChannel(
                            "whoami",
                            LIBSSH2_CHANNEL_EXTENDED_DATA.NORMAL));
                }
            }
        }

        [Test]
        public async Task WhenConnected_ThenOpenShellChannelAsyncChannelSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instanceLocator = await instanceLocatorTask;
            var endpoint = new IPEndPoint(
                await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                22);
            using (var key = new RsaSshKey(new RSACng()))
            {
                await InstanceUtil.AddPublicKeyToMetadata(
                    instanceLocator,
                    "testuser",
                    key).ConfigureAwait(true);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                using (var authSession = connection.Authenticate("testuser", key))
                using (var channel = authSession.OpenExecChannel(
                    "whoami",
                    LIBSSH2_CHANNEL_EXTENDED_DATA.NORMAL))
                {
                    channel.Close();
                }
            }
        }
    }
}
