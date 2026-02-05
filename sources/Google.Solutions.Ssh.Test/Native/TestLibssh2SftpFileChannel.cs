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

using Google.Solutions.Apis.Locator;
using Google.Solutions.Ssh.Cryptography;
using Google.Solutions.Ssh.Native;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test.Native
{
    [TestFixture]
    [UsesCloudResources]
    public class TestLibssh2SftpFileChannel : SshFixtureBase
    {
        //---------------------------------------------------------------------
        // Attributes.
        //---------------------------------------------------------------------

        [Test]
        public async Task Attributes_WhenFileIsNew(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            var fileName = Guid.NewGuid().ToString();

            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            using (var authSession = connection.Authenticate(
                credential,
                new KeyboardInteractiveHandler()))
            using (var channel = authSession.OpenSftpChannel())
            using (var file = channel.CreateFile(
                fileName,
                LIBSSH2_FXF_FLAGS.CREAT | LIBSSH2_FXF_FLAGS.WRITE,
                FilePermissions.OwnerRead | FilePermissions.OwnerWrite))
            {
                var attributes = file.Attributes;

                Assert.That(attributes.flags.HasFlag(LIBSSH2_SFTP_ATTR.SIZE), Is.True);
                Assert.That(attributes.filesize, Is.EqualTo(0));

                Assert.That(attributes.flags.HasFlag(LIBSSH2_SFTP_ATTR.PERMISSIONS), Is.True);
                Assert.That(
                    attributes.permissions, Is.EqualTo(FilePermissions.OwnerRead |
                        FilePermissions.OwnerWrite |
                        FilePermissions.Regular));
            }
        }

        [Test]
        public async Task Attributes_WhenFileExists(
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
            using (var channel = authSession.OpenSftpChannel())
            using (var file = channel.CreateFile(
                "/etc/passwd",
                LIBSSH2_FXF_FLAGS.READ,
                FilePermissions.None))
            {
                var attributes = file.Attributes;

                Assert.That(attributes.flags.HasFlag(LIBSSH2_SFTP_ATTR.SIZE), Is.True);
                Assert.That(attributes.filesize, Is.Not.EqualTo(0));

                Assert.That(attributes.flags.HasFlag(LIBSSH2_SFTP_ATTR.ACMODTIME), Is.True);
                Assert.That(attributes.atime, Is.Not.EqualTo(0));
            }
        }

        //---------------------------------------------------------------------
        // Read.
        //---------------------------------------------------------------------

        [Test]
        public async Task Read_WhenFileSizeIsZero_ThenReadReturnsZero(
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
            using (var channel = authSession.OpenSftpChannel())
            using (var file = channel.CreateFile(
                Guid.NewGuid().ToString(),
                LIBSSH2_FXF_FLAGS.CREAT | LIBSSH2_FXF_FLAGS.READ,
                FilePermissions.OwnerRead | FilePermissions.OwnerWrite))
            {
                var bytesRead = file.Read(new byte[16]);
                Assert.That(bytesRead, Is.EqualTo(0));
            }
        }

        //---------------------------------------------------------------------
        // Write.
        //---------------------------------------------------------------------

        [Test]
        public async Task Write_WhenFileWritten_ThenReadReturnsSameData(
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
            using (var channel = authSession.OpenSftpChannel())
            {
                var sendData = new StringBuilder();
                for (var i = 0; i < 500; i++)
                {
                    sendData.Append(i);
                    sendData.Append("The quick brown fox jumps over the lazy dog\n");
                }

                var fileName = Guid.NewGuid().ToString();

                using (var file = channel.CreateFile(
                    fileName,
                    LIBSSH2_FXF_FLAGS.CREAT | LIBSSH2_FXF_FLAGS.WRITE,
                    FilePermissions.OwnerRead | FilePermissions.OwnerWrite))
                {
                    var buffer = Encoding.ASCII.GetBytes(sendData.ToString());
                    file.Write(buffer, buffer.Length);
                }

                using (var file = channel.CreateFile(
                    fileName,
                    LIBSSH2_FXF_FLAGS.READ,
                    FilePermissions.OwnerRead | FilePermissions.OwnerWrite))
                {
                    var receiveData = new StringBuilder();

                    uint bytesRead = 0;
                    var tinyBuffer = new byte[128];
                    while ((bytesRead = file.Read(tinyBuffer)) > 0)
                    {
                        receiveData.Append(Encoding.ASCII.GetString(tinyBuffer, 0, (int)bytesRead));
                    }

                    Assert.That(receiveData.ToString(), Is.EqualTo(sendData.ToString()));
                }
            }
        }
    }
}
