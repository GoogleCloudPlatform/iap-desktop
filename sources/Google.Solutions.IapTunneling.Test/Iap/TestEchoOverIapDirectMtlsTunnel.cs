//
// Copyright 2019 Google LLC
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

using Google.Apis.Auth.OAuth2;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.IapDesktop.Application.Services.SecureConnect;
using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.IapTunneling.Net;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Test.Iap
{
    [TestFixture]
    [Category("IntegrationTest")]
    [Category("IAP")]
    public class TestMtls
    {
        [Test]
        public async Task __test()
        {
            var adapter = new SecureConnectAdapter();

            // TODO: Use test user ID.
            var enrollment = adapter.GetEnrollmentInfoAsync("113269283503306052571").Result;
            Assert.IsNotNull(enrollment.DeviceCertificate);

            var stream = new FragmentingStream(new SshRelayStream(
                new IapTunnelingEndpoint(
                    GoogleCredential.GetApplicationDefault(),
                    new InstanceLocator("ntdev-caa", "us-central1-a", "win-mtls"),
                    3389,
                    IapTunnelingEndpoint.DefaultNetworkInterface,
                    TestProject.UserAgent,
                    enrollment.DeviceCertificate)));

            var message = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            await stream.WriteAsync(message, 0, message.Length, CancellationToken.None);

            var response = new byte[message.Length];
            int totalBytesRead = 0;
            while (true)
            {
                var bytesRead = await stream.ReadAsync(
                    response,
                    totalBytesRead,
                    response.Length - totalBytesRead,
                    CancellationToken.None);
                totalBytesRead += bytesRead;

                if (bytesRead == 0 || totalBytesRead >= response.Length)
                {
                    break;
                }
            }

            Assert.AreEqual(response.Length, totalBytesRead);
            Assert.AreEqual(message, response);

            await stream.CloseAsync(CancellationToken.None);
        }
    }
}
