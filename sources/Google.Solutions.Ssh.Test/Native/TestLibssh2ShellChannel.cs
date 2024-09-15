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

using Google.Solutions.Apis.Locator;
using Google.Solutions.Ssh.Cryptography;
using Google.Solutions.Ssh.Native;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test.Native
{
    [TestFixture]
    [UsesCloudResources]
    public class TestLibssh2ShellChannel : SshFixtureBase
    {
        private const string DefaultTerminal = "vanilla";

        private static string ReadToEnd(
            Libssh2ChannelBase channel,
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

        private static string ReadUntil(
            Libssh2ChannelBase channel,
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

        //---------------------------------------------------------------------
        // Shell.
        //---------------------------------------------------------------------

        [Test]
        public async Task OpenShellChannel_WhenConnected(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            using (var authSession = connection.Authenticate(
                credential,
                new KeyboardInteractiveHandler()))
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
                    $"whoami;exit\r\n{credential.Username}\r\nlogout\r\n",
                    output);

                Assert.AreEqual(0, channel.ExitCode);
                Assert.AreEqual(null, channel.ExitSignal);
            }
        }

        [Test]
        public async Task OpenShellChannel_WhenWhitelistedEnvironmentVariablePassed_ThenShellCanAccessVariable(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            using (var authSession = connection.Authenticate(
                credential,
                new KeyboardInteractiveHandler()))
            using (var channel = authSession.OpenShellChannel(
                LIBSSH2_CHANNEL_EXTENDED_DATA.MERGE,
                DefaultTerminal,
                80,
                24,
                new[]
                {
                    new EnvironmentVariable(
                        "LC_ALL",
                        "en_AU",
                        true) // LC_* is whitelisted by sshd by default.
                }))
            {
                var bytesWritten = channel.Write(Encoding.ASCII.GetBytes("echo $LANG;exit\n"));
                Assert.AreEqual(16, bytesWritten);

                var output = ReadToEnd(channel, Encoding.ASCII);
                channel.Close();

                //
                // The locale might be unknown, but then there'll be an error by
                // setlocale. In either case, we should find "en_AU" somewhere in
                // the output, confirming that it has been passed to the VM.
                //
                StringAssert.Contains(
                    "en_AU",
                    output);

                Assert.AreEqual(0, channel.ExitCode);
            }
        }

        [Test]
        public async Task OpenShellChannel_WhenNonWhitelistedEnvironmentVariablePassed_ThenOpenShellChannelAsyncThrowsRequestDenied(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            using (var authSession = connection.Authenticate(
                credential,
                new KeyboardInteractiveHandler()))
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

        [Test]
        public async Task OpenShellChannel_WhenPseudoterminalResized_ThenShellReflectsNewSize(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            using (var authSession = connection.Authenticate(
                credential,
                new KeyboardInteractiveHandler()))
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
