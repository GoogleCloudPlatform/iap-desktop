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
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test
{
    [TestFixture]
    [UsesCloudResources]
    public class TestSshFileSystemChannel : SshFixtureBase
    {
        public static Stream CreateStream(string content)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        //---------------------------------------------------------------------
        // Close.
        //---------------------------------------------------------------------

        [Test]
        public async Task Close_WhenChannelClosedExplicitly_ThenDoubleCloseIsPrevented(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            using (var connection = new SshConnection(
                endpoint,
                credential,
                new KeyboardInteractiveHandler()))
            {
                await connection
                    .ConnectAsync()
                    .ConfigureAwait(false);

                using (var channel = await connection.OpenFileSystemAsync()
                    .ConfigureAwait(false))
                {
                }
            }
        }

        //---------------------------------------------------------------------
        // ListFiles.
        //---------------------------------------------------------------------

        [Test]
        public async Task ListFiles_WhenPathExists_ThenListFilesAsyncReturnsFiles(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            using (var connection = new SshConnection(
                endpoint,
                credential,
                new KeyboardInteractiveHandler()))
            {
                await connection
                    .ConnectAsync()
                    .ConfigureAwait(false);

                var channel = await connection.OpenFileSystemAsync()
                    .ConfigureAwait(false);

                var files = await channel
                    .ListFilesAsync("/etc")
                    .ConfigureAwait(false);

                Assert.IsNotNull(files);
                CollectionAssert.IsNotEmpty(files);
            }
        }

        [Test]
        public async Task ListFiles_WhenPathInvalid_ThenListFilesAsyncThrowsException(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            using (var connection = new SshConnection(
                endpoint,
                credential,
                new KeyboardInteractiveHandler()))
            {
                await connection
                    .ConnectAsync()
                    .ConfigureAwait(false);

                var channel = await connection.OpenFileSystemAsync()
                    .ConfigureAwait(false);

                SshAssert.ThrowsAggregateExceptionWithError(
                    LIBSSH2_FX_ERROR.NO_SUCH_FILE,
                    () => channel
                        .ListFilesAsync("/does-not-exist")
                        .Wait());
            }
        }

        //---------------------------------------------------------------------
        // UploadFile.
        //---------------------------------------------------------------------

        [Test]
        public async Task UploadFile_WhenFileDoesNotExistYet_ThenUploadFileSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            using (var connection = new SshConnection(
                endpoint,
                credential,
                new KeyboardInteractiveHandler()))
            {
                await connection
                    .ConnectAsync()
                    .ConfigureAwait(false);

                var channel = await connection.OpenFileSystemAsync()
                    .ConfigureAwait(false);

                var fileName = $"{Guid.NewGuid()}.txt";
                using (var data = CreateStream("This is some test data"))
                {
                    await channel.UploadFileAsync(
                            fileName,
                            data,
                            LIBSSH2_FXF_FLAGS.CREAT | LIBSSH2_FXF_FLAGS.WRITE,
                            FilePermissions.OtherRead | FilePermissions.OwnerWrite,
                            new Progress<uint>(),
                            CancellationToken.None)
                        .ConfigureAwait(false);
                }
            }
        }

        [Test]
        public async Task UploadFile_WhenFileExists_ThenUploadFileThrowsException(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            using (var connection = new SshConnection(
                endpoint,
                credential,
                new KeyboardInteractiveHandler()))
            {
                await connection
                    .ConnectAsync()
                    .ConfigureAwait(false);

                var channel = await connection.OpenFileSystemAsync()
                    .ConfigureAwait(false);

                using (var data = CreateStream("This is some test data"))
                {
                    SshAssert.ThrowsAggregateExceptionWithError(
                        LIBSSH2_FX_ERROR.PERMISSION_DENIED,
                        () => channel.UploadFileAsync(
                                "/etc/passwd",
                                data,
                                LIBSSH2_FXF_FLAGS.CREAT | LIBSSH2_FXF_FLAGS.WRITE,
                                FilePermissions.OtherRead | FilePermissions.OwnerWrite,
                                new Progress<uint>(),
                                CancellationToken.None)
                            .Wait());
                }
            }
        }

        [Test]
        public async Task UploadFile_WhenUploadCancelled_ThenUploadFileThrowsException(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            using (var connection = new SshConnection(
                endpoint,
                credential,
                new KeyboardInteractiveHandler()))
            {
                await connection
                    .ConnectAsync()
                    .ConfigureAwait(false);

                var channel = await connection.OpenFileSystemAsync()
                    .ConfigureAwait(false);

                var fileName = $"{Guid.NewGuid()}.txt";
                using (var cts = new CancellationTokenSource())
                using (var data = CreateStream("This is some test data"))
                {
                    cts.Cancel();

                    ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                        () => channel.UploadFileAsync(
                                fileName,
                                data,
                                LIBSSH2_FXF_FLAGS.CREAT | LIBSSH2_FXF_FLAGS.WRITE,
                                FilePermissions.OtherRead | FilePermissions.OwnerWrite,
                                new Progress<uint>(),
                                cts.Token)
                            .Wait());
                }
            }
        }

        //---------------------------------------------------------------------
        // DownloadFile.
        //---------------------------------------------------------------------

        [Test]
        public async Task DownloadFile_WhenFileExists_ThenDownloadFileSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            using (var connection = new SshConnection(
                endpoint,
                credential,
                new KeyboardInteractiveHandler()))
            {
                await connection
                    .ConnectAsync()
                    .ConfigureAwait(false);

                var channel = await connection.OpenFileSystemAsync()
                    .ConfigureAwait(false);

                using (var data = new MemoryStream())
                {
                    await channel.DownloadFileAsync(
                            "/etc/passwd",
                            data,
                            new Progress<uint>(),
                            CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.AreNotEqual(0, data.Length);
                }
            }
        }

        [Test]
        public async Task DownloadFile_WhenFileDoesNotExist_ThenDownloadFileThrowsException(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            using (var connection = new SshConnection(
                endpoint,
                credential,
                new KeyboardInteractiveHandler()))
            {
                await connection
                    .ConnectAsync()
                    .ConfigureAwait(false);

                var channel = await connection.OpenFileSystemAsync()
                    .ConfigureAwait(false);

                using (var data = new MemoryStream())
                {
                    SshAssert.ThrowsAggregateExceptionWithError(
                        LIBSSH2_FX_ERROR.NO_SUCH_FILE,
                        () => channel.DownloadFileAsync(
                                "/this/file-does-not-exist.txt",
                                data,
                                new Progress<uint>(),
                                CancellationToken.None)
                            .Wait());
                }
            }
        }

        [Test]
        public async Task DownloadFile_WhenDownloadCancelled_ThenDownloadFileThrowsException(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            using (var connection = new SshConnection(
                endpoint,
                credential,
                new KeyboardInteractiveHandler()))
            {
                await connection
                    .ConnectAsync()
                    .ConfigureAwait(false);

                var channel = await connection.OpenFileSystemAsync()
                    .ConfigureAwait(false);

                using (var cts = new CancellationTokenSource())
                using (var data = new MemoryStream())
                {
                    cts.Cancel();

                    ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                        () => channel.DownloadFileAsync(
                                "/etc/passwd",
                                data,
                                new Progress<uint>(),
                                cts.Token)
                            .Wait());
                }
            }
        }
    }
}
