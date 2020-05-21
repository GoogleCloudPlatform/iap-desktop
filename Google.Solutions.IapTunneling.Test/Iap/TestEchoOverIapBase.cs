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

using Google.Solutions.Common;
using Google.Solutions.Common.Test.Testbed;
using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.IapTunneling.Net;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Test.Iap
{
    public abstract class TestEchoOverIapBase : FixtureBase
    {
        private const string InstallEchoServer =
            "apt-get install -y xinetd          \n" +
            "cat << EOF > /etc/xinetd.d/echo    \n" +
            "service echo                       \n" +
            "{                                  \n" +
            "    disable         = no           \n" +
            "    type            = INTERNAL     \n" +
            "    id              = echo-stream  \n" +
            "    socket_type     = stream       \n" +
            "    protocol        = tcp          \n" +
            "    user            = root         \n" +
            "    wait            = no           \n" +
            "}                                  \n" +
            "EOF\n" +
            "\n" +
            "service xinetd restart";

        private const int RepeatCount = 10;
        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        protected abstract INetworkStream ConnectToEchoServer(VmInstanceReference vmRef);

        private static void FillArray(byte[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = (byte)('A' + (i % 26));
            }
        }

        [Test]
        public async Task WhenSendingMessagesToEchoServer_MessagesAreReceivedVerbatim(
            [LinuxInstance(InitializeScript = InstallEchoServer)] InstanceRequest vm,
            [Values(
                1,
                (int)DataMessage.MaxDataLength - 1,
                (int)DataMessage.MaxDataLength,
                (int)DataMessage.MaxDataLength + 1,
                (int)DataMessage.MaxDataLength * 10)] int length,
            [Values(1, 3)] int count)
        {

            var message = new byte[length];
            FillArray(message);

            await vm.AwaitReady();
            var stream = ConnectToEchoServer(vm.InstanceReference);

            for (int i = 0; i < count; i++)
            {
                await stream.WriteAsync(message, 0, message.Length, this.tokenSource.Token);

                var response = new byte[length];
                int totalBytesRead = 0;
                while (true)
                {
                    var bytesRead = await stream.ReadAsync(
                        response,
                        totalBytesRead,
                        response.Length - totalBytesRead,
                        this.tokenSource.Token);
                    totalBytesRead += bytesRead;

                    if (bytesRead == 0 || totalBytesRead >= length)
                    {
                        break;
                    }
                }

                Assert.AreEqual(length, totalBytesRead);
                Assert.AreEqual(message, response);
            }

            await stream.CloseAsync(this.tokenSource.Token);
        }
    }
}
