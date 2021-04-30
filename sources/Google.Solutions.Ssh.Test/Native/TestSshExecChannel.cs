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
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test.Native
{

    [TestFixture]
    public class TestSshExecChannel : SshFixtureBase
    {
        private string UnexpectedAuthenticationCallback(
            string name,
            string instruction,
            string prompt,
            bool echo)
        {
            Assert.Fail("Unexpected callback");
            return null;
        }

        //---------------------------------------------------------------------
        // Exec.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenCommandIsValid_ThenOpenExecChannelAsyncSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                22);
            using (var key = new RsaSshKey(new RSACng()))
            {
                await InstanceUtil.AddPublicKeyToMetadata(
                    await instanceLocatorTask,
                    "testuser",
                    key).ConfigureAwait(true);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                using (var authSession = connection.Authenticate(
                    "testuser", 
                    key,
                    UnexpectedAuthenticationCallback))
                using (var channel = authSession.OpenExecChannel(
                    "whoami",
                    LIBSSH2_CHANNEL_EXTENDED_DATA.NORMAL))
                {
                    channel.WaitForEndOfStream();

                    var buffer = new byte[1024];
                    var bytesRead = channel.Read(buffer);
                    Assert.AreNotEqual(0, bytesRead);

                    Assert.AreEqual("testuser\n", Encoding.ASCII.GetString(buffer, 0, (int)bytesRead));

                    Assert.AreEqual(0, channel.ExitCode);
                    Assert.IsNull(channel.ExitSignal);
                    channel.Close();
                }
            }
        }

        [Test]
        public async Task WhenNoMoreDataToRead_ThenReadReturnZero(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                22);
            using (var key = new RsaSshKey(new RSACng()))
            {
                await InstanceUtil.AddPublicKeyToMetadata(
                    await instanceLocatorTask,
                    "testuser",
                    key).ConfigureAwait(true);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                using (var authSession = connection.Authenticate(
                    "testuser", 
                    key,
                    UnexpectedAuthenticationCallback))
                using (var channel = authSession.OpenExecChannel(
                    "whoami",
                    LIBSSH2_CHANNEL_EXTENDED_DATA.NORMAL))
                {
                    channel.WaitForEndOfStream();

                    Assert.AreNotEqual(0, channel.Read(new byte[1024]));
                    Assert.AreEqual(0, channel.Read(new byte[1024]));
                    channel.Close();
                }
            }
        }

        [Test]
        public async Task WhenCommandInvalidAndExtendedDataModeIsNormal_ThenExecuteSucceedsAndStderrContainsError(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                22);
            using (var key = new RsaSshKey(new RSACng()))
            {
                await InstanceUtil.AddPublicKeyToMetadata(
                    await instanceLocatorTask,
                    "testuser",
                    key).ConfigureAwait(true);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                using (var authSession = connection.Authenticate(
                    "testuser", 
                    key,
                    UnexpectedAuthenticationCallback))
                using (var channel = authSession.OpenExecChannel(
                    "invalidcommand",
                    LIBSSH2_CHANNEL_EXTENDED_DATA.NORMAL))
                {
                    channel.WaitForEndOfStream();

                    var buffer = new byte[1024];
                    var bytesRead = channel.Read(
                        buffer,
                        LIBSSH2_STREAM.EXTENDED_DATA_STDERR);
                    Assert.AreNotEqual(0, bytesRead);

                    Assert.AreEqual(
                        "bash: invalidcommand: command not found\n",
                        Encoding.ASCII.GetString(buffer, 0, (int)bytesRead));

                    Assert.AreEqual(127, channel.ExitCode);
                    Assert.IsNull(channel.ExitSignal);
                    channel.Close();
                }
            }
        }

        [Test]
        public async Task WhenCommandInvalidAndExtendedDataModeIsMerge_ThenExecuteSucceedsAndStdoutContainsError(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                22);
            using (var key = new RsaSshKey(new RSACng()))
            {
                await InstanceUtil.AddPublicKeyToMetadata(
                    await instanceLocatorTask,
                    "testuser",
                    key).ConfigureAwait(true);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                using (var authSession = connection.Authenticate(
                    "testuser", 
                    key,
                    UnexpectedAuthenticationCallback))
                using (var channel = authSession.OpenExecChannel(
                    "invalidcommand",
                    LIBSSH2_CHANNEL_EXTENDED_DATA.MERGE))
                {
                    channel.WaitForEndOfStream();

                    var buffer = new byte[1024];
                    var bytesRead = channel.Read(
                        buffer);
                    Assert.AreNotEqual(0, bytesRead);

                    Assert.AreEqual(
                        "bash: invalidcommand: command not found\n",
                        Encoding.ASCII.GetString(buffer, 0, (int)bytesRead));

                    Assert.IsNull(channel.ExitSignal);
                    channel.Close();
                }
            }
        }
    }
}
