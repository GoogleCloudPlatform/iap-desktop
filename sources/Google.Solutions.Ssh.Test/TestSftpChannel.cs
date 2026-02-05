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
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test
{
    [TestFixture]
    [UsesCloudResources]
    public class TestSftpChannel : SshFixtureBase
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
        // CreateFile.
        //---------------------------------------------------------------------

        [Test]
        public async Task CreateFile_CreateNew_WhenFileExists(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance)
                .ConfigureAwait(false);
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

                using (var channel = await connection
                    .OpenFileSystemAsync()
                    .ConfigureAwait(false))
                using (var bufferStream = new MemoryStream())
                {
                    ExceptionAssert.ThrowsAggregateException<Libssh2SftpException>(
                        () => channel
                            .CreateFileAsync(
                                "/etc/passwd",
                                FileMode.CreateNew,
                                FileAccess.Read,
                                FilePermissions.None)
                            .Wait());
                }
            }
        }

        [Test]
        public async Task CreateFile_WriteReadAsynchronously(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance)
                .ConfigureAwait(false);
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

                using (var channel = await connection
                    .OpenFileSystemAsync()
                    .ConfigureAwait(false))
                {
                    var tempFile = $"{Guid.NewGuid()}.txt";

                    //
                    // Write to temp file.
                    //
                    using (var outputStream = await channel
                        .CreateFileAsync(
                            tempFile,
                            FileMode.Create,
                            FileAccess.Write,
                            FilePermissions.OwnerWrite | FilePermissions.OwnerRead)
                        .ConfigureAwait(false))
                    {
                        Assert.IsTrue(outputStream.CanWrite);
                        Assert.IsFalse(outputStream.CanRead);
                        Assert.IsFalse(outputStream.CanSeek);

                        Assert.That(outputStream.Length, Is.EqualTo(0));

                        ExceptionAssert.ThrowsAggregateException<NotSupportedException>(
                            () => outputStream.ReadAsync(new byte[1], 0, 1).Wait());

                        var data = Encoding.ASCII.GetBytes("'Some data'");
                        await outputStream
                            .WriteAsync(data, 1, data.Length - 2)
                            .ConfigureAwait(false);

                        Assert.That(outputStream.Position, Is.EqualTo(data.Length - 2));
                    }

                    //
                    // Read back.
                    //
                    using (var inputStream = await channel
                        .CreateFileAsync(
                            tempFile,
                            FileMode.Open,
                            FileAccess.Read,
                            FilePermissions.None)
                        .ConfigureAwait(false))
                    {
                        Assert.IsTrue(inputStream.CanRead);
                        Assert.IsFalse(inputStream.CanWrite);
                        Assert.IsFalse(inputStream.CanSeek);

                        Assert.AreNotEqual(0, inputStream.Length);

                        ExceptionAssert.ThrowsAggregateException<NotSupportedException>(
                            () => inputStream.WriteAsync(new byte[1], 0, 1).Wait());

                        var buffer = new byte[1024];
                        var bytesRead = await inputStream
                            .ReadAsync(buffer, 1, buffer.Length - 1)
                            .ConfigureAwait(false);

                        Assert.That(
                            Encoding.ASCII.GetString(buffer, 1, bytesRead), Is.EqualTo("Some data"));
                        Assert.That(inputStream.Position, Is.EqualTo(bytesRead));

                        bytesRead = await inputStream
                            .ReadAsync(buffer, 0, buffer.Length)
                            .ConfigureAwait(false);
                        Assert.That(bytesRead, Is.EqualTo(0));
                    }
                }
            }
        }

        [Test]
        public async Task CreateFile_WriteReadSynchronously(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance)
                .ConfigureAwait(false);
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

                using (var channel = await connection
                    .OpenFileSystemAsync()
                    .ConfigureAwait(false))
                {
                    var tempFile = $"{Guid.NewGuid()}.txt";

                    //
                    // Write to temp file.
                    //
                    using (var outputStream = await channel
                        .CreateFileAsync(
                            tempFile,
                            FileMode.Create,
                            FileAccess.Write,
                            FilePermissions.OwnerWrite | FilePermissions.OwnerRead)
                        .ConfigureAwait(false))
                    {
                        Assert.IsTrue(outputStream.CanWrite);
                        Assert.IsFalse(outputStream.CanRead);
                        Assert.IsFalse(outputStream.CanSeek);

                        Assert.That(outputStream.Length, Is.EqualTo(0));

                        Assert.Throws<NotSupportedException>(
                            () => _ = outputStream.Read(new byte[1], 0, 1));

                        var data = Encoding.ASCII.GetBytes("'Some data'");
                        outputStream.Write(data, 1, data.Length - 2);

                        Assert.That(outputStream.Position, Is.EqualTo(data.Length - 2));
                    }

                    //
                    // Read back.
                    //
                    using (var inputStream = await channel
                        .CreateFileAsync(
                            tempFile,
                            FileMode.Open,
                            FileAccess.Read,
                            FilePermissions.None)
                        .ConfigureAwait(false))
                    {
                        Assert.IsTrue(inputStream.CanRead);
                        Assert.IsFalse(inputStream.CanWrite);
                        Assert.IsFalse(inputStream.CanSeek);

                        Assert.AreNotEqual(0, inputStream.Length);

                        Assert.Throws<NotSupportedException>(
                            () => inputStream.Write(new byte[1], 0, 1));

                        var buffer = new byte[1024];
                        var bytesRead = inputStream.Read(buffer, 1, buffer.Length - 1);

                        Assert.That(
                            Encoding.ASCII.GetString(buffer, 1, bytesRead), Is.EqualTo("Some data"));
                        Assert.That(inputStream.Position, Is.EqualTo(bytesRead));

                        bytesRead = inputStream.Read(buffer, 0, buffer.Length);
                        Assert.That(bytesRead, Is.EqualTo(0));
                    }
                }
            }
        }
    }
}
