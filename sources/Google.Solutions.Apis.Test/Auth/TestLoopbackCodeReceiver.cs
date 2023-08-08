﻿//
// Copyright 2023 Google LLC
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
using Google.Apis.Auth.OAuth2.Requests;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Client;
using Google.Solutions.Testing.Apis;
using Moq;
using NUnit.Framework;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Test.Auth
{
    [TestFixture]
    public class TestLoopbackCodeReceiver
    {
        private static readonly Uri SampleUri = new Uri("http://auth.example.com/");

        private class Receiver : LoopbackCodeReceiver
        {
            public Action<string> OpenBrowserCallback = _ => { };

            public Receiver(string path, string responseHtml) 
                : base(path, responseHtml)
            {
            }

            protected override void OpenBrowser(string url)
            {
                this.OpenBrowserCallback(url);
            }
        }

        //---------------------------------------------------------------------
        // ReceiveCode.
        //---------------------------------------------------------------------

        [Test]
        public void WhenCancelledBeforeListen_ThenReceiveCodeThrowsException()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                var receiver = new Receiver("/", "done")
                {
                    OpenBrowserCallback = _ => tokenSource.Cancel()
                };

                var url = new AuthorizationCodeRequestUrl(SampleUri);

                ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                    () => receiver.ReceiveCodeAsync(url, tokenSource.Token).Wait());
            }
        }

        [Test]
        public void WhenCancelledAfterListen_ThenReceiveCodeThrowsException()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                var receiver = new Receiver("/", "done");
                var url = new AuthorizationCodeRequestUrl(SampleUri);

                var receiveTask = receiver.ReceiveCodeAsync(url, tokenSource.Token);
                
                tokenSource.Cancel();

                ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                    () => receiveTask.Wait());
            }
        }

        [Test]
        public async Task WhenCodeReceived_ThenReceiveCodeReturns()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                var receiver = new Receiver("/", "done");
                var url = new AuthorizationCodeRequestUrl(SampleUri);

                var receiveTask = receiver.ReceiveCodeAsync(url, tokenSource.Token);
                using (var client = new HttpClient())
                {
                    var htmlResponse = await client
                        .GetAsync($"{receiver.RedirectUri}?code=c1&state=s1")
                        .ConfigureAwait(false);

                    var html = await htmlResponse.Content
                        .ReadAsStringAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual("done", html);
                }

                var response = await receiveTask.ConfigureAwait(false);

                Assert.AreEqual("c1", response.Code);
                Assert.AreEqual("s1", response.State);
            }
        }

        [Test]
        public async Task WhenErrorReceived_ThenReceiveCodeReturns()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                var receiver = new Receiver("/", "done");
                var url = new AuthorizationCodeRequestUrl(SampleUri);

                var receiveTask = receiver.ReceiveCodeAsync(url, tokenSource.Token);
                using (var client = new HttpClient())
                {
                    var htmlResponse = await client
                        .GetAsync($"{receiver.RedirectUri}?error=c1&state")
                        .ConfigureAwait(false);

                    var html = await htmlResponse.Content
                        .ReadAsStringAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual("done", html);
                }

                var response = await receiveTask.ConfigureAwait(false);

                Assert.AreEqual("c1", response.Error);
                Assert.IsNull(response.State);
            }
        }
    }
}
