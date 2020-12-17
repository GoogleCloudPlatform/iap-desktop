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
            using (var session = CreateSession())
            using (var connection = await session.ConnectAsync(endpoint))
            using (var key = new RSACng())
            {
                await InstanceUtil.AddPublicKeyToMetadata(
                    await instanceLocatorTask,
                    "testuser",
                    key);

                using (var authSession = await connection.AuthenticateAsync("testuser", key))
                using (var channel = await authSession.OpenExecChannelAsync(
                    "whoami",
                    LIBSSH2_CHANNEL_EXTENDED_DATA.NORMAL))
                {
                    await channel.CloseAsync();

                    var buffer = new byte[1024];
                    var bytesRead = await channel.ReadAsync(buffer);
                    Assert.AreNotEqual(0, bytesRead);

                    Assert.AreEqual("testuser\n", Encoding.ASCII.GetString(buffer, 0, (int)bytesRead));

                    Assert.AreEqual(0, channel.ExitCode);
                    Assert.IsNull(channel.ExitSignal);
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
            using (var session = CreateSession())
            using (var connection = await session.ConnectAsync(endpoint))
            using (var key = new RSACng())
            {
                await InstanceUtil.AddPublicKeyToMetadata(
                    await instanceLocatorTask,
                    "testuser",
                    key);

                using (var authSession = await connection.AuthenticateAsync("testuser", key))
                using (var channel = await authSession.OpenExecChannelAsync(
                    "whoami",
                    LIBSSH2_CHANNEL_EXTENDED_DATA.NORMAL))
                {
                    await channel.CloseAsync();

                    Assert.AreNotEqual(0, await channel.ReadAsync(new byte[1024]));
                    Assert.AreEqual(0, await channel.ReadAsync(new byte[1024]));
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
            using (var session = CreateSession())
            using (var connection = await session.ConnectAsync(endpoint))
            using (var key = new RSACng())
            {
                await InstanceUtil.AddPublicKeyToMetadata(
                    await instanceLocatorTask,
                    "testuser",
                    key);

                using (var authSession = await connection.AuthenticateAsync("testuser", key))
                using (var channel = await authSession.OpenExecChannelAsync(
                    "invalidcommand",
                    LIBSSH2_CHANNEL_EXTENDED_DATA.NORMAL))
                {
                    await channel.CloseAsync();

                    var buffer = new byte[1024];
                    var bytesRead = await channel.ReadAsync(
                        LIBSSH2_STREAM.EXTENDED_DATA_STDERR,
                        buffer);
                    Assert.AreNotEqual(0, bytesRead);

                    Assert.AreEqual(
                        "bash: invalidcommand: command not found\n",
                        Encoding.ASCII.GetString(buffer, 0, (int)bytesRead));

                    Assert.AreEqual(127, channel.ExitCode);
                    Assert.IsNull(channel.ExitSignal);
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
            using (var session = CreateSession())
            using (var connection = await session.ConnectAsync(endpoint))
            using (var key = new RSACng())
            {
                await InstanceUtil.AddPublicKeyToMetadata(
                    await instanceLocatorTask,
                    "testuser",
                    key);

                using (var authSession = await connection.AuthenticateAsync("testuser", key))
                using (var channel = await authSession.OpenExecChannelAsync(
                    "invalidcommand",
                    LIBSSH2_CHANNEL_EXTENDED_DATA.MERGE))
                {
                    await channel.CloseAsync();

                    var buffer = new byte[1024];
                    var bytesRead = await channel.ReadAsync(
                        buffer);
                    Assert.AreNotEqual(0, bytesRead);

                    Assert.AreEqual(
                        "bash: invalidcommand: command not found\n",
                        Encoding.ASCII.GetString(buffer, 0, (int)bytesRead));

                    Assert.AreEqual(127, channel.ExitCode);
                    Assert.IsNull(channel.ExitSignal);
                }
            }
        }

        //---------------------------------------------------------------------
        // I/O.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenStreamFlushed_ThenReadReturnsZero(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                22);
            using (var session = CreateSession())
            using (var connection = await session.ConnectAsync(endpoint))
            using (var key = new RSACng())
            {
                await InstanceUtil.AddPublicKeyToMetadata(
                    await instanceLocatorTask,
                    "testuser",
                    key);

                using (var authSession = await connection.AuthenticateAsync("testuser", key))
                using (var channel = await authSession.OpenExecChannelAsync(
                    "whoami",
                    LIBSSH2_CHANNEL_EXTENDED_DATA.NORMAL))
                {
                    await channel.CloseAsync();

                    // Read first byte of output.
                    await channel.ReadAsync(new byte[1]);

                    // Flush the rest of the read buffer.
                    Assert.AreNotEqual(0, channel.Flush());
                    Assert.AreEqual(0, channel.Flush());

                    var bytesRead = await channel.ReadAsync(new byte[1024]);
                    Assert.AreEqual(0, bytesRead);
                }
            }
        }
    }
}
