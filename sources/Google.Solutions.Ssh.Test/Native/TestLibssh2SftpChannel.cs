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
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test.Native
{
    [TestFixture]
    [UsesCloudResources]
    public class TestLibssh2SftpChannel : SshFixtureBase
    {
        //---------------------------------------------------------------------
        // Open/Close.
        //---------------------------------------------------------------------

        [Test]
        public async Task OpenSftpChannel_WhenSftpDisabled_ThenOpenSftpChannelThrowsException(
            [LinuxInstance(InitializeScript =
                "sed -i '/.*sftp-server/d' /etc/ssh/sshd_config && systemctl restart sshd")]
            ResourceTask<InstanceLocator> instanceLocatorTask)
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
                    LIBSSH2_ERROR.CHANNEL_FAILURE,
                    () => authSession.OpenSftpChannel());
            }
        }

        [Test]
        public async Task OpenSftpChannel_WhenAuthenticated_ThenOpenSftpChannelReturnsChannel(
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
                Assert.That(channel, Is.Not.Null);
            }
        }

        //---------------------------------------------------------------------
        // ListFiles.
        //---------------------------------------------------------------------

        [Test]
        public async Task ListFiles_WhenDirectoryDoesNotExist_ThenListFilesThrowsException(
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
                SshAssert.ThrowsSftpNativeExceptionWithError(
                    LIBSSH2_FX_ERROR.NO_SUCH_FILE,
                    () => channel.ListFiles("/this/does/not/exist"));
            }
        }

        [Test]
        public async Task ListFiles_WhenDirectoryExists_ThenListFilesReturnsFiles(
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
                var files = channel.ListFiles("/etc");

                Assert.That(files, Is.Not.Null);
                Assert.That(files.Count, Is.GreaterThan(1));

                var passwd = files.First(f => f.Name == "passwd");
                Assert.That(passwd.Permissions.HasFlag(FilePermissions.Regular), Is.True);
                Assert.That(passwd.IsDirectory, Is.False);
            }
        }

        [Test]
        public async Task ListFiles_WhenPathIsDot_ThenListFilesReturnsFilesFromHomeDirectory(
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
                var files = channel.ListFiles(".");

                Assert.That(files, Is.Not.Null);
                Assert.That(files.Count, Is.GreaterThan(1));

                var parent = files.First(f => f.Name == "..");
                Assert.That(parent.Permissions.HasFlag(FilePermissions.Directory), Is.True);
                Assert.That(parent.IsDirectory, Is.True);
            }
        }

        //---------------------------------------------------------------------
        // CreateDirectory.
        //---------------------------------------------------------------------

        [Test]
        public async Task CreateDirectory_WhenParentDirectoryDoesNotExist_ThenCreateDirectoryThrowsException(
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
                SshAssert.ThrowsSftpNativeExceptionWithError(
                    LIBSSH2_FX_ERROR.NO_SUCH_FILE,
                    () => channel.CreateDirectory(
                        "/this/does/not/exist",
                        FilePermissions.OwnerExecute |
                            FilePermissions.OwnerRead |
                            FilePermissions.OtherWrite));
            }
        }

        [Test]
        public async Task CreateDirectory_WhenParentDirectoryNotWritable_ThenCreateDirectoryThrowsException(
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
                SshAssert.ThrowsSftpNativeExceptionWithError(
                    LIBSSH2_FX_ERROR.PERMISSION_DENIED,
                    () => channel.CreateDirectory(
                        "/dev/cant-create-a-file-in-dev",
                        FilePermissions.OwnerExecute |
                            FilePermissions.OwnerRead |
                            FilePermissions.OtherWrite));
            }
        }

        [Test]
        public async Task CreateDirectory_WhenParentDirectoryExists_ThenCreateDirectorySucceeds(
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
                var directoryName = Guid.NewGuid().ToString();
                channel.CreateDirectory(directoryName,
                    FilePermissions.OwnerExecute |
                        FilePermissions.OwnerRead |
                        FilePermissions.OtherWrite);

                Assert.That(channel
                    .ListFiles(".")
                    .Any(f => f.Name == directoryName), Is.True,
                    "Directory created");
            }
        }

        //---------------------------------------------------------------------
        // DeleteDirectory.
        //---------------------------------------------------------------------

        [Test]
        public async Task DeleteDirectory_WhenDirectoryDoesNotExist_ThenDeleteDirectoryThrowsException(
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
                SshAssert.ThrowsSftpNativeExceptionWithError(
                    LIBSSH2_FX_ERROR.NO_SUCH_FILE,
                    () => channel.DeleteDirectory("/this/does/not/exist"));
            }
        }

        [Test]
        public async Task DeleteDirectory_WhenDirectoryExists_ThenDeleteDirectorySucceeds(
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
                var directoryName = Guid.NewGuid().ToString();
                channel.CreateDirectory(directoryName,
                    FilePermissions.OwnerExecute |
                        FilePermissions.OwnerRead |
                        FilePermissions.OtherWrite);
                channel.DeleteDirectory(directoryName);

                Assert.That(channel
                    .ListFiles(".")
                    .Any(f => f.Name == directoryName), Is.False,
                    "Directory deleted");
            }
        }

        //---------------------------------------------------------------------
        // CreateFile.
        //---------------------------------------------------------------------

        [Test]
        public async Task CreateFile_WhenParentDirectoryDoesNotExist_ThenCreateFileThrowsException(
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
                SshAssert.ThrowsSftpNativeExceptionWithError(
                    LIBSSH2_FX_ERROR.NO_SUCH_FILE,
                    () => channel.CreateFile(
                        "/this/does/not/exist",
                        LIBSSH2_FXF_FLAGS.CREAT,
                        FilePermissions.OwnerExecute |
                            FilePermissions.OwnerRead |
                            FilePermissions.OtherWrite));
            }
        }

        [Test]
        public async Task CreateFile_WhenParentDirectoryNotWritable_ThenCreateFileThrowsException(
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
                SshAssert.ThrowsSftpNativeExceptionWithError(
                    LIBSSH2_FX_ERROR.PERMISSION_DENIED,
                    () => channel.CreateFile(
                        "/dev/cant-create-a-file-in-dev",
                        LIBSSH2_FXF_FLAGS.CREAT,
                        FilePermissions.OwnerExecute |
                            FilePermissions.OwnerRead |
                            FilePermissions.OtherWrite));
            }
        }

        [Test]
        public async Task CreateFile_WhenParentDirectoryExists_ThenCreateFileSucceeds(
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
                LIBSSH2_FXF_FLAGS.CREAT,
                FilePermissions.OwnerExecute |
                    FilePermissions.OwnerRead |
                    FilePermissions.OtherWrite))
            {
                Assert.That(channel
                    .ListFiles(".")
                    .Any(f => f.Name == fileName), Is.True,
                    "File created");
            }
        }

        //---------------------------------------------------------------------
        // DeleteFile.
        //---------------------------------------------------------------------

        [Test]
        public async Task DeleteFile_WhenFileDoesNotExist_ThenDeleteFileThrowsException(
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
                SshAssert.ThrowsSftpNativeExceptionWithError(
                    LIBSSH2_FX_ERROR.NO_SUCH_FILE,
                    () => channel.DeleteFile("/this/does/not/exist"));
            }
        }

        [Test]
        public async Task DeleteFile_WhenFileIsDirectory_ThenDeleteFileThrowsException(
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
                SshAssert.ThrowsSftpNativeExceptionWithError(
                    LIBSSH2_FX_ERROR.FAILURE,
                    () => channel.DeleteFile("."));
            }
        }

        [Test]
        public async Task DeleteFile_WhenFileExists_ThenDeleteFileSucceeds(
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
                var fileName = Guid.NewGuid().ToString();

                using (var file = channel.CreateFile(
                    fileName,
                    LIBSSH2_FXF_FLAGS.CREAT,
                    FilePermissions.OwnerExecute |
                        FilePermissions.OwnerRead |
                        FilePermissions.OtherWrite))
                { }

                channel.DeleteFile(fileName);

                Assert.That(channel
                    .ListFiles(".")
                    .Any(f => f.Name == fileName), Is.False,
                    "File deleted");
            }
        }
    }
}
