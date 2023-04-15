﻿//
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
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapTunneling.Net;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Test.Iap
{
    public abstract class TestEchoOverIapBase : IapFixtureBase
    {
        private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();

        protected abstract INetworkStream ConnectToEchoServer(
            InstanceLocator vmRef,
            ICredential credential);

        private static void FillArray(byte[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = (byte)('A' + (i % 26));
            }
        }

        protected async Task WhenSendingMessagesToEchoServer_MessagesAreReceivedVerbatim(
            InstanceLocator locator,
            ICredential credential,
            int messageSize,
            int writeSize,
            int readSize,
            int repetitions)
        {

            var message = new byte[messageSize];
            FillArray(message);

            var stream = ConnectToEchoServer(
                locator,
                credential);

            for (int i = 0; i < repetitions; i++)
            {
                int totalBytesWritten = 0;
                while (totalBytesWritten < messageSize)
                {
                    var bytesToWrite = Math.Min(
                        writeSize,
                        messageSize - totalBytesWritten);
                    await stream
                        .WriteAsync(
                            message,
                            totalBytesWritten,
                            bytesToWrite,
                            this.tokenSource.Token)
                        .ConfigureAwait(false);
                    totalBytesWritten += bytesToWrite;
                }


                var response = new byte[messageSize];
                int totalBytesRead = 0;
                while (true)
                {
                    var readBuffer = new byte[readSize];
                    var bytesRead = await stream
                        .ReadAsync(
                            readBuffer,
                            0,
                            readSize,
                            this.tokenSource.Token)
                        .ConfigureAwait(false);

                    Array.Copy(readBuffer, 0, response, totalBytesRead, bytesRead);
                    totalBytesRead += bytesRead;

                    if (bytesRead == 0 || totalBytesRead >= messageSize)
                    {
                        break;
                    }
                }

                Assert.AreEqual(messageSize, totalBytesRead);
                Assert.AreEqual(message, response);
            }

            await stream
                .CloseAsync(this.tokenSource.Token)
                .ConfigureAwait(false);
        }
    }
}
