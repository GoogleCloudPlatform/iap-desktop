﻿//
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
    public class TestSshSftpFileChannel : SshFixtureBase
    {
        //---------------------------------------------------------------------
        // Read.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenFileSizeIsZero_ThenReadReturnsZero(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var authenticator = await CreateEphemeralAuthenticatorForInstanceAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            using (var authSession = connection.Authenticate(authenticator))
            using (var channel = authSession.OpenSftpChannel())
            using (var file = channel.CreateFile(
                Guid.NewGuid().ToString(),
                LIBSSH2_FXF_FLAGS.CREAT | LIBSSH2_FXF_FLAGS.READ,
                FilePermissions.OwnerExecute |
                    FilePermissions.OwnerRead |
                    FilePermissions.OtherWrite))
            {
                var bytesRead = file.Read(new byte[16]);
                Assert.AreEqual(0, bytesRead);
            }
        }

        //---------------------------------------------------------------------
        // Write.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenFileWritten_ThenReadReturnsSameData(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var authenticator = await CreateEphemeralAuthenticatorForInstanceAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            using (var authSession = connection.Authenticate(authenticator))
            using (var channel = authSession.OpenSftpChannel())
            {
                var sendData = new StringBuilder();
                for (int i = 0; i < 500; i++)
                {
                    sendData.Append(i);
                    sendData.Append("The quick brown fox jumps over the lazy dog\n");
                }

                var fileName = Guid.NewGuid().ToString();

                using (var file = channel.CreateFile(
                    fileName,
                    LIBSSH2_FXF_FLAGS.CREAT | LIBSSH2_FXF_FLAGS.WRITE,
                    FilePermissions.OwnerExecute |
                        FilePermissions.OwnerRead |
                        FilePermissions.OtherWrite))
                {
                    var buffer = Encoding.ASCII.GetBytes(sendData.ToString());
                    file.Write(buffer, buffer.Length);
                }

                using (var file = channel.CreateFile(
                    fileName,
                    LIBSSH2_FXF_FLAGS.READ,
                    FilePermissions.OwnerExecute |
                        FilePermissions.OwnerRead |
                        FilePermissions.OtherWrite))
                {
                    var receiveData = new StringBuilder();

                    uint bytesRead = 0;
                    var tinyBuffer = new byte[128];
                    while ((bytesRead = file.Read(tinyBuffer)) > 0)
                    {
                        receiveData.Append(Encoding.ASCII.GetString(tinyBuffer, 0, (int)bytesRead));
                    }

                    Assert.AreEqual(sendData.ToString(), receiveData.ToString());
                }
            }
        }
    }
}
