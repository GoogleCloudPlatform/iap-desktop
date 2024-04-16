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
        public async Task WhenSftpDisabledAuthenticated_ThenOpenSftpChannelThrowsException(
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
        public async Task WhenAuthenticated_ThenOpenSftpChannelReturnsChannel(
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
        public async Task WhenDirectoryExists_ThenListFilesReturnsFiles(
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

                Assert.NotNull(files);
                Assert.Greater(files.Count, 1);

                var passwd = files.First(f => f.Name == "passwd");
                Assert.IsNotNull(passwd);
                Assert.IsTrue(passwd.Permissions.HasFlag(FilePermissions.Regular));
                Assert.IsFalse(passwd.IsDirectory);
            }
        }

        [Test]
        public async Task WhenPathIsDot_ThenListFilesReturnsFilesFromHomeDirectory(
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

                Assert.NotNull(files);
                Assert.Greater(files.Count, 1);

                var parent = files.First(f => f.Name == "..");
                Assert.IsNotNull(parent);
                Assert.IsTrue(parent.Permissions.HasFlag(FilePermissions.Directory));
                Assert.IsTrue(parent.IsDirectory);
            }
        }

        //---------------------------------------------------------------------
        // CreateDirectory.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenParentDirectoryDoesNotExist_ThenCreateDirectoryThrowsException(
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
        public async Task WhenParentDirectoryNotWritable_ThenCreateDirectoryThrowsException(
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
        public async Task WhenParentDirectoryExists_ThenCreateDirectorySucceeds(
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

                Assert.IsTrue(channel
                    .ListFiles(".")
                    .Any(f => f.Name == directoryName),
                    "Directory created");
            }
        }

        //---------------------------------------------------------------------
        // DeleteDirectory.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenDirectoryDoesNotExist_ThenDeleteDirectoryThrowsException(
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
        public async Task WhenDirectoryExists_ThenDeleteDirectorySucceeds(
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

                Assert.IsFalse(channel
                    .ListFiles(".")
                    .Any(f => f.Name == directoryName),
                    "Directory deleted");
            }
        }

        //---------------------------------------------------------------------
        // CreateFile.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenParentDirectoryDoesNotExist_ThenCreateFileThrowsException(
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
        public async Task WhenParentDirectoryNotWritable_ThenCreateFileThrowsException(
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
        public async Task WhenParentDirectoryExists_ThenCreateFileSucceeds(
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
                Assert.IsTrue(channel
                    .ListFiles(".")
                    .Any(f => f.Name == fileName),
                    "File created");
            }
        }

        //---------------------------------------------------------------------
        // DeleteFile.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenFileDoesNotExist_ThenDeleteFileThrowsException(
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
        public async Task WhenFileIsDirectory_ThenDeleteFileThrowsException(
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
        public async Task WhenFileExists_ThenDeleteFileSucceeds(
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

                Assert.IsFalse(channel
                    .ListFiles(".")
                    .Any(f => f.Name == fileName),
                    "File deleted");
            }
        }
    }
}
