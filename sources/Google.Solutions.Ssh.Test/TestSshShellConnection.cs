//
// Copyright 2020 Google LLC
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
using Google.Solutions.Common.Test;
using Google.Solutions.Common.Test.Integration;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test
{
    [TestFixture]
    public class TestSshShellConnection : SshFixtureBase
    {
        private async Task AwaitBufferAsync(
            StringBuilder buffer,
            TimeSpan timeout,
            int minimumLength)
        {
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(timeout.TotalMilliseconds / 10));
                if (buffer.Length > minimumLength)
                {
                    return;
                }
            }

            throw new TimeoutException(
                "Timeout waiting for buffer to contain at least " +
                $"{minimumLength} characters of data");
        }

        private async Task AwaitBufferContentAsync(
            StringBuilder buffer,
            TimeSpan timeout,
            string token)
        {
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(timeout.TotalMilliseconds / 10));

                lock (buffer)
                {
                    if (buffer.ToString().Contains(token))
                    {
                        return;
                    }
                }
            }

            throw new TimeoutException(
                $"Timeout waiting for buffer to contain '{token}");
        }

        [Test]
        public async Task WhenSendingEchoCommand_ThenEchoIsReceived(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                22);

            using (var key = new RsaSshKey(new RSACng()))
            {
                await InstanceUtil.AddPublicKeyToMetadata(
                    await instanceLocatorTask,
                    "testuser",
                    key);

                var receiveBuffer = new StringBuilder();
                SshShellConnection.ReceiveStringDataHandler receiveHandler = data =>
                {
                    lock (receiveBuffer)
                    {
                        receiveBuffer.Append(data);
                    }
                };

                using (var connection = new SshShellConnection(
                    "testuser",
                    endpoint,
                    key,
                    SshShellConnection.DefaultTerminal,
                    SshShellConnection.DefaultTerminalSize,
                    CultureInfo.InvariantCulture,
                    receiveHandler,
                    exception =>
                    {
                        Assert.Fail("Unexpected error");
                    }))
                {
                    await connection.ConnectAsync();

                    AssertEx.ThrowsAggregateException<InvalidOperationException>(
                        () => connection.ConnectAsync().Wait());

                    await connection.SendAsync("whoami\n");
                    await connection.SendAsync("exit\n");

                    await AwaitBufferContentAsync(
                        receiveBuffer,
                        TimeSpan.FromSeconds(10),
                        "testuser");

                    StringAssert.Contains(
                        "testuser",
                        receiveBuffer.ToString());
                }
            }
        }
    }
}
