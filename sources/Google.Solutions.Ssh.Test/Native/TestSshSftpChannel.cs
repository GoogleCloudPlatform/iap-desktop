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
    public class TestSshSftpChannel : SshFixtureBase
    {
        //---------------------------------------------------------------------
        // Open/Close.
        //---------------------------------------------------------------------

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
    }
}
