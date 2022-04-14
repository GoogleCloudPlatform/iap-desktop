//
// Copyright 2022 Google LLC
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
using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test.Native
{
    [TestFixture]
    public class TestSshSftpChannel : SshFixtureBase
    {
        //---------------------------------------------------------------------
        // Open/Close.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSftpDisabledAuthenticated_ThenOpenSftpChannelThrowsException(
            [LinuxInstance(InitializeScript =
                "sed -i '/.*sftp-server/d' /etc/ssh/sshd_config && systemctl restart sshd")]
            ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            using (var key = await InstanceUtil
               .CreateEphemeralKeyAndPushKeyToMetadata(instance, "testuser", SshKeyType.Rsa3072)
               .ConfigureAwait(false))
            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            using (var authSession = connection.Authenticate(
                "testuser",
                key,
                UnexpectedAuthenticationCallback))
            {
                SshAssert.ThrowsNativeExceptionWithError(
                    session,
                    LIBSSH2_ERROR.CHANNEL_FAILURE,
                    () => authSession.OpenSftpChannel());
            }
        }

        [Test]
        public async Task WhenAuthenticated_ThenOpenSftpChannelReturnsChannel(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            using (var key = await InstanceUtil
               .CreateEphemeralKeyAndPushKeyToMetadata(instance, "testuser", SshKeyType.Rsa3072)
               .ConfigureAwait(false))
            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            using (var authSession = connection.Authenticate(
                "testuser",
                key,
                UnexpectedAuthenticationCallback))
            using (var channel = authSession.OpenSftpChannel())
            {
                Assert.IsNotNull(channel);
            }
        }

        //---------------------------------------------------------------------
        // ListFiles.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenDirectoryDoesNotExist_ThenListFilesThrowsException(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            using (var key = await InstanceUtil
               .CreateEphemeralKeyAndPushKeyToMetadata(instance, "testuser", SshKeyType.Rsa3072)
               .ConfigureAwait(false))
            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            using (var authSession = connection.Authenticate(
                "testuser",
                key,
                UnexpectedAuthenticationCallback))
            using (var channel = authSession.OpenSftpChannel())
            {
                SshAssert.ThrowsSftpNativeExceptionWithErrno(
                    2,
                    () => channel.ListFiles("/this/does/not/exist"));
            }
        }

        [Test]
        public async Task WhenDirectoryExists_ThenListFilesReturnsFiles(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            using (var key = await InstanceUtil
               .CreateEphemeralKeyAndPushKeyToMetadata(instance, "testuser", SshKeyType.Rsa3072)
               .ConfigureAwait(false))
            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            using (var authSession = connection.Authenticate(
                "testuser",
                key,
                UnexpectedAuthenticationCallback))
            using (var channel = authSession.OpenSftpChannel())
            {
                var files = channel.ListFiles("/etc");

                Assert.NotNull(files);
                Assert.Greater(files.Count, 1);

                var passwd = files.First(f => f.Name == "passwd");
                Assert.IsNotNull(passwd);
                Assert.IsTrue(passwd.Permissions.HasFlag(FilePermissions.Regular));
                Assert.IsFalse(passwd.IsDirectory);
            }
        }
    }
}
