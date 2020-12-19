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
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test.Native
{

    [TestFixture]
    public class TestSshShellChannel : SshFixtureBase
    {
        private const string DefaultTerminal = "vanilla";

        private async Task<string> ReadToEndAsync(
            SshChannelBase channel,
            Encoding encoding)
        {
            var text = new StringBuilder();
            var buffer = new byte[1024];
            
            while (!channel.IsEndOfStream)
            {
                uint bytesRead = await channel.ReadAsync(buffer);
                if (bytesRead > 0)
                {
                    text.Append(encoding.GetString(buffer, 0, (int)bytesRead));
                }
            }

            return text.ToString();
        }

        private async Task<string> ReadUntilAsync(
            SshChannelBase channel,
            string delimiter,
            Encoding encoding)
        {
            var text = new StringBuilder();

            var buffer = new byte[1];

            while ((await channel.ReadAsync(buffer)) > 0)
            {
                var ch = encoding.GetString(buffer, 0, 1);
                text.Append(ch);

                if (text.ToString().EndsWith(delimiter))
                {
                    return text.ToString();
                }
            }

            return text.ToString();
        }

        //---------------------------------------------------------------------
        // Shell.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnected_ThenOpenShellChannelAsyncSucceeds(
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
                using (var channel = await authSession.OpenShellChannelAsync(
                    LIBSSH2_CHANNEL_EXTENDED_DATA.MERGE,
                    DefaultTerminal,
                    80,
                    24))
                {
                    // Run command.
                    var bytesWritten = await channel.WriteAsync(Encoding.ASCII.GetBytes("whoami;exit\n"));
                    Assert.AreEqual(12, bytesWritten);

                    await channel.CloseAsync();

                    // Read command output.
                    var output = await ReadToEndAsync(channel, Encoding.ASCII);
                    StringAssert.Contains(
                        "whoami;exit\r\ntestuser\r\nlogout\r\n",
                        output);

                    Assert.AreEqual(0, channel.ExitCode);
                    Assert.AreEqual(null, channel.ExitSignal);
                }
            }
        }

        [Test]
        public async Task WhenWhitelistedEnvironmentVariablePassed_ThenShellCanAccessVariable(
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
                using (var channel = await authSession.OpenShellChannelAsync(
                    LIBSSH2_CHANNEL_EXTENDED_DATA.MERGE,
                    DefaultTerminal,
                    80,
                    24,
                    new Dictionary<string, string>
                    {
                        { "LANG", "LC_ALL" } // LANG is whitelisted by sshd by default.
                    }))
                {
                    var bytesWritten = await channel.WriteAsync(Encoding.ASCII.GetBytes("echo $LANG;exit\n"));
                    Assert.AreEqual(16, bytesWritten);

                    await channel.CloseAsync();

                    var output = await ReadToEndAsync(channel, Encoding.ASCII);

                    StringAssert.Contains(
                        "en_US.UTF-8",
                        output);

                    Assert.AreEqual(0, channel.ExitCode);
                }
            }
        }

        [Test]
        public async Task WhenNonWhitelistedEnvironmentVariablePassed_ThenOpenShellChannelAsyncThrowsRequestDenied(
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
                {
                    SshAssert.ThrowsNativeExceptionWithError(
                        session,
                        LIBSSH2_ERROR.CHANNEL_REQUEST_DENIED,
                        () => authSession.OpenShellChannelAsync(
                            LIBSSH2_CHANNEL_EXTENDED_DATA.MERGE,
                            DefaultTerminal,
                            80,
                            24,
                            new Dictionary<string, string>
                            {
                                { "FOO", "foo" },
                                { "BAR", "bar" }
                            }).Wait());
                }
            }
        }

        [Test]
        public async Task WhenPseudoterminalResized_ThenShellReflectsNewSize(
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
                using (var channel = await authSession.OpenShellChannelAsync(
                    LIBSSH2_CHANNEL_EXTENDED_DATA.MERGE,
                    DefaultTerminal,
                    80,
                    24))
                {
                    var welcome = await ReadUntilAsync(channel, "~$", Encoding.ASCII);

                    // Read initial terminal size.
                    await channel.WriteAsync(Encoding.ASCII.GetBytes("echo $COLUMNS $LINES\n"));
                    await ReadUntilAsync(channel, "\n", Encoding.ASCII);

                    var terminalSize = await ReadUntilAsync(channel, "\n", Encoding.ASCII);
                    Assert.AreEqual("80 24\r\n", terminalSize);

                    // Resize terminal.
                    await channel.ResizePseudoTerminal(100, 30);

                    // Read terminal size again.
                    await channel.WriteAsync(Encoding.ASCII.GetBytes("echo $COLUMNS $LINES\n"));
                    await ReadUntilAsync(channel, "\n", Encoding.ASCII);

                    terminalSize = await ReadUntilAsync(channel, "\n", Encoding.ASCII);
                    Assert.AreEqual("100 30\r\n", terminalSize);

                    await channel.CloseAsync();
                }
            }
        }
    }
}
