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

using Google.Apis.Requests;
using Google.Apis.Util;
using Google.Solutions.Common.ApiExtensions.Request;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Test.Extensions
{
    [TestFixture]
    public class TestRetryExtensions : FixtureBase
    {
        [Test]
        public void WhenRequestFailsWith429_ThenRequestIsRetried()
        {
            var request = new Mock<IClientServiceRequest<string>>();
            request.Setup(r => r.ExecuteAsStreamAsync())
                .Returns(Task.FromException<Stream>(new GoogleApiException("mock", "message")
                {
                    Error = new RequestError()
                    {
                        Code = 429
                    }
                }));

            var backoff = new ExponentialBackOff(TimeSpan.FromMilliseconds(1), 3);

            AssertEx.ThrowsAggregateException<GoogleApiException>(
                () => request.Object.ExecuteAsStreamWithRetryAsync(backoff).Wait());

            request.Verify(r => r.ExecuteAsStreamAsync(), Times.Exactly(backoff.MaxNumOfRetries + 1));
        }

        [Test]
        public void WhenRequestFailsWithoutErrorDetails_ThenRequestIsNotRetried()
        {

            var request = new Mock<IClientServiceRequest<string>>();
            request.Setup(r => r.ExecuteAsStreamAsync())
                .Returns(Task.FromException<Stream>(new GoogleApiException("mock", "message")));

            var backoff = new ExponentialBackOff(TimeSpan.FromMilliseconds(1), 3);

            AssertEx.ThrowsAggregateException<GoogleApiException>(
                () => request.Object.ExecuteAsStreamWithRetryAsync(backoff).Wait());

            request.Verify(r => r.ExecuteAsStreamAsync(), Times.Exactly(1));
        }
    }
}