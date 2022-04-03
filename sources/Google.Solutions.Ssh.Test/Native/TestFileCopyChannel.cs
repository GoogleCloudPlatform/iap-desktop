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
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test.Native
{
    [TestFixture]
    public class TestFileCopyChannel : SshFixtureBase
    {
        private const string SshdConfigPath = "/etc/ssh/sshd_config";

        // TODO: Reuse in other classes
        private static async Task<IPEndPoint> GetEndpointAsync(
            InstanceLocator instance)
        {
            return new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(instance)
                    .ConfigureAwait(false),
                22);
        }

        // TODO: Reuse in other classes
        private static async Task PushKeyAsync(
            InstanceLocator instance,
            ISshKeyPair key)
        {
            await InstanceUtil
                .AddPublicKeyToMetadata(
                    instance,
                    "testuser",
                    key)
                .ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // Downloading.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenFileDoesNotExist_ThenOpenFileDownloadChannelThrowsException(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetEndpointAsync(instance).ConfigureAwait(false);

            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072))
            {
                await PushKeyAsync(instance, key).ConfigureAwait(false);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                using (var authSession = connection.Authenticate(
                    "testuser",
                    key,
                    UnexpectedAuthenticationCallback))
                {
                    SshAssert.ThrowsNativeExceptionWithError(
                        session,
                        LIBSSH2_ERROR.SCP_PROTOCOL,
                        () => authSession.OpenFileDownloadChannel(
                            $"/doesnotexist/{Guid.NewGuid()}.txt"));
                }
            }
        }

        [Test]
        public async Task WhenFileExists_ThenOpenFileDownloadChannelReturnsChannel(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetEndpointAsync(instance).ConfigureAwait(false);

            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072))
            {
                await PushKeyAsync(instance, key).ConfigureAwait(false);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                using (var authSession = connection.Authenticate(
                    "testuser",
                    key,
                    UnexpectedAuthenticationCallback))
                using (var download = authSession.OpenFileDownloadChannel(SshdConfigPath))
                {
                    Assert.Greater(download.FileSize, 0);

                    var tinyBuffer = new byte[8];
                    uint totalBytesRead = 0;
                    uint bytesRead = 0;
                    while ((bytesRead = download.Read(tinyBuffer)) > 0)
                    {
                        totalBytesRead += bytesRead;
                    }

                    Assert.AreEqual(download.FileSize, totalBytesRead);
                }
            }
        }

        [Test]
        public async Task WhenCreatingSubsequentDownloadChannel_ThenOpenFileDownloadChannelSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetEndpointAsync(instance).ConfigureAwait(false);

            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072))
            {
                await PushKeyAsync(instance, key).ConfigureAwait(false);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                using (var authSession = connection.Authenticate(
                    "testuser",
                    key,
                    UnexpectedAuthenticationCallback))
                {
                    // Start first download.
                    using (var download1 = authSession.OpenFileDownloadChannel(SshdConfigPath))
                    {
                        Assert.Greater(download1.FileSize, 0);
                    }

                    // Start another download.
                    using (var download2 = authSession.OpenFileDownloadChannel(SshdConfigPath))
                    {
                        Assert.Greater(download2.FileSize, 0);
                    }
                }
            }
        }

        [Test]
        public async Task WhenWritingToDownloadChannel_ThenWriteThrowsException(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetEndpointAsync(instance).ConfigureAwait(false);

            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072))
            {
                await PushKeyAsync(instance, key).ConfigureAwait(false);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                using (var authSession = connection.Authenticate(
                    "testuser",
                    key,
                    UnexpectedAuthenticationCallback))
                using (var download = authSession.OpenFileDownloadChannel(SshdConfigPath))
                {
                    SshAssert.ThrowsNativeExceptionWithError(
                        session,
                        LIBSSH2_ERROR.ALLOC,
                        () => download.Write(new byte[8]));
                }
            }
        }

        //---------------------------------------------------------------------
        // Uploading.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenFileSizeIsZero_ThenOpenFileUploadChannelReturnsChannel(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetEndpointAsync(instance).ConfigureAwait(false);

            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072))
            {
                await PushKeyAsync(instance, key).ConfigureAwait(false);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                using (var authSession = connection.Authenticate(
                    "testuser",
                    key,
                    UnexpectedAuthenticationCallback))
                using (var upload = authSession.OpenFileUploadChannel(
                    $"~/{Guid.NewGuid()}.txt",
                    0600,
                    0))
                {
                }
            }
        }

        [Test]
        public async Task WhenParentFolderDoesNotExist_ThenOpenFileUploadChannelThrowsException(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetEndpointAsync(instance).ConfigureAwait(false);

            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072))
            {
                await PushKeyAsync(instance, key).ConfigureAwait(false);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                using (var authSession = connection.Authenticate(
                    "testuser",
                    key,
                    UnexpectedAuthenticationCallback))
                {
                    SshAssert.ThrowsNativeExceptionWithError(
                        session,
                        LIBSSH2_ERROR.ALLOC,
                        () => authSession.OpenFileUploadChannel(
                            $"/doesnotexist/{Guid.NewGuid()}/file.txt",
                            0600,
                            1));
                }
            }
        }

        [Test]
        public async Task WhenCreatingSubsequentUploadChannel_ThenOpenFileUploadChannelSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetEndpointAsync(instance).ConfigureAwait(false);

            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072))
            {
                await PushKeyAsync(instance, key).ConfigureAwait(false);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                using (var authSession = connection.Authenticate(
                    "testuser",
                    key,
                    UnexpectedAuthenticationCallback))
                {
                    var remotePath = $"~/{Guid.NewGuid()}.txt";
                    using (var upload1 = authSession.OpenFileUploadChannel(
                        remotePath,
                        0600,
                        1))
                    {
                        upload1.Write(new byte[] { (byte)'A' });
                    }

                    using (var upload2 = authSession.OpenFileUploadChannel(
                        remotePath,
                        0600,
                        1))
                    {
                        upload2.Write(new byte[] { (byte)'B' });
                    }
                }
            }
        }

        [Test]
        public async Task WhenFileUploaded_ThenDownloadSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetEndpointAsync(instance).ConfigureAwait(false);

            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072))
            {
                await PushKeyAsync(instance, key).ConfigureAwait(false);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                using (var authSession = connection.Authenticate(
                    "testuser",
                    key,
                    UnexpectedAuthenticationCallback))
                {
                    var remotePath = $"~/{Guid.NewGuid()}.txt";

                    var text = "This is some data";
                    var encodedText = Encoding.ASCII.GetBytes(text);

                    using (var upload = authSession.OpenFileUploadChannel(
                        remotePath,
                        0600,
                        encodedText.Length))
                    {
                        upload.Write(encodedText);
                    }

                    using (var download = authSession.OpenFileDownloadChannel(remotePath))
                    {
                        Assert.AreEqual(encodedText.Length, download.FileSize);

                        var buffer = new byte[encodedText.Length];
                        Assert.AreEqual(encodedText.Length, download.Read(buffer));

                        Assert.AreEqual(
                            text,
                            Encoding.ASCII.GetString(buffer));
                    }
                }
            }
        }

        [Test]
        public async Task WhenReadingFromUploadChannel_ThenReadThrowsException(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetEndpointAsync(instance).ConfigureAwait(false);

            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072))
            {
                await PushKeyAsync(instance, key).ConfigureAwait(false);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                using (var authSession = connection.Authenticate(
                    "testuser",
                    key,
                    UnexpectedAuthenticationCallback))
                using (var upload = authSession.OpenFileUploadChannel(
                    $"~/{Guid.NewGuid()}.txt",
                    0600,
                    0))
                {
                    SshAssert.ThrowsNativeExceptionWithError(
                        session,
                        LIBSSH2_ERROR.ALLOC,
                        () => upload.Read(new byte[8]));

                }
            }
        }
    }
}
