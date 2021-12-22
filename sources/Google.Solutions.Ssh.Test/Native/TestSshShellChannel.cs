﻿//
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
using Google.Solutions.Ssh.Auth;
using Google.Solutions.Ssh.Native;
using NUnit.Framework;
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

        private string ReadToEnd(
            SshChannelBase channel,
            Encoding encoding)
        {
            channel.WaitForEndOfStream();

            var text = new StringBuilder();
            var buffer = new byte[1024];
            uint bytesRead;
            while ((bytesRead = channel.Read(buffer)) > 0)
            {
                text.Append(encoding.GetString(buffer, 0, (int)bytesRead));
            }

            return text.ToString();
        }

        private string ReadUntil(
            SshChannelBase channel,
            string delimiter,
            Encoding encoding)
        {
            var text = new StringBuilder();
            var buffer = new byte[1];

            while ((channel.Read(buffer)) > 0)
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
        // Shell.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnected_ThenOpenShellChannelAsyncSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);

            using (var key = SshKey.NewEphemeralKey(SshKeyType.Rsa3072))
            {
                await InstanceUtil
                    .AddPublicKeyToMetadata(
                        await instanceLocatorTask,
                        "testuser",
                        key)
                    .ConfigureAwait(false);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                using (var authSession = connection.Authenticate(
                    "testuser", 
                    key,
                    UnexpectedAuthenticationCallback))
                using (var channel = authSession.OpenShellChannel(
                    LIBSSH2_CHANNEL_EXTENDED_DATA.MERGE,
                    DefaultTerminal,
                    80,
                    24))
                {
                    // Run command.
                    var bytesWritten = channel.Write(Encoding.ASCII.GetBytes("whoami;exit\n"));
                    Assert.AreEqual(12, bytesWritten);

                    // Read command output.
                    var output = ReadToEnd(channel, Encoding.ASCII);
                    channel.Close();

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
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);
            using (var key = SshKey.NewEphemeralKey(SshKeyType.Rsa3072))
            {
                await InstanceUtil
                    .AddPublicKeyToMetadata(
                        await instanceLocatorTask,
                        "testuser",
                        key)
                    .ConfigureAwait(false);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                using (var authSession = connection.Authenticate(
                    "testuser", 
                    key,
                    UnexpectedAuthenticationCallback))
                using (var channel = authSession.OpenShellChannel(
                    LIBSSH2_CHANNEL_EXTENDED_DATA.MERGE,
                    DefaultTerminal,
                    80,
                    24,
                    new[]
                    {
                        new EnvironmentVariable(
                            "LANG",
                            "LC_ALL",
                            true) // LANG is whitelisted by sshd by default.
                    }))
                {
                    var bytesWritten = channel.Write(Encoding.ASCII.GetBytes("echo $LANG;exit\n"));
                    Assert.AreEqual(16, bytesWritten);

                    var output = ReadToEnd(channel, Encoding.ASCII);
                    channel.Close();

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
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);
            using (var key = SshKey.NewEphemeralKey(SshKeyType.Rsa3072))
            {
                await InstanceUtil
                    .AddPublicKeyToMetadata(
                        await instanceLocatorTask,
                        "testuser",
                        key)
                    .ConfigureAwait(false);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                using (var authSession = connection.Authenticate(
                    "testuser", 
                    key,
                    UnexpectedAuthenticationCallback))
                {
                    SshAssert.ThrowsNativeExceptionWithError(
                        session,
                        LIBSSH2_ERROR.CHANNEL_REQUEST_DENIED,
                        () => authSession.OpenShellChannel(
                            LIBSSH2_CHANNEL_EXTENDED_DATA.MERGE,
                            DefaultTerminal,
                            80,
                            24,
                            new[]
                            {
                                new EnvironmentVariable("FOO", "foo", true),
                                new EnvironmentVariable("BAR", "bar", true)
                            }));
                }
            }
        }

        [Test]
        public async Task WhenPseudoterminalResized_ThenShellReflectsNewSize(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);
            using (var key = SshKey.NewEphemeralKey(SshKeyType.Rsa3072))
            {
                await InstanceUtil
                    .AddPublicKeyToMetadata(
                        await instanceLocatorTask,
                        "testuser",
                        key)
                    .ConfigureAwait(false);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                using (var authSession = connection.Authenticate(
                    "testuser", 
                    key,
                    UnexpectedAuthenticationCallback))
                using (var channel = authSession.OpenShellChannel(
                    LIBSSH2_CHANNEL_EXTENDED_DATA.MERGE,
                    DefaultTerminal,
                    80,
                    24))
                {
                    var welcome = ReadUntil(channel, "~$", Encoding.ASCII);

                    // Read initial terminal size.
                    channel.Write(Encoding.ASCII.GetBytes("echo $COLUMNS $LINES\n"));
                    ReadUntil(channel, "\n", Encoding.ASCII);

                    var terminalSize = ReadUntil(channel, "\n", Encoding.ASCII);
                    Assert.AreEqual("80 24\r\n", terminalSize);

                    // Resize terminal.
                    channel.ResizePseudoTerminal(100, 30);

                    // Read terminal size again.
                    channel.Write(Encoding.ASCII.GetBytes("echo $COLUMNS $LINES\n"));
                    ReadUntil(channel, "\n", Encoding.ASCII);

                    terminalSize = ReadUntil(channel, "\n", Encoding.ASCII);
                    Assert.AreEqual("100 30\r\n", terminalSize);

                    channel.Close();
                }
            }
        }
    }
}
