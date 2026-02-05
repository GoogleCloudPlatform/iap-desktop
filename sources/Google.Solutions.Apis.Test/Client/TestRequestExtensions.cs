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
using Google.Apis.Compute.v1;
using Google.Apis.Compute.v1.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using Google.Solutions.Apis.Client;
using Google.Solutions.Common.Test;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using Moq;
using NUnit.Framework;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Test.Client
{
    [TestFixture]
    [UsesCloudResources]
    public class TestExecuteAsStreamExtensions : CommonFixtureBase
    {
        [Test]
        public async Task ExecuteAsStreamOrThrow_WhenApiReturns404(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential
            )
        {
            var computeService = new ComputeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = await credential
            });

            await ExceptionAssert
                .ThrowsAsync<GoogleApiException>(() => computeService.Instances
                    .Get("invalid", "invalid", "invalid")
                    .ExecuteAsStreamOrThrowAsync(CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ExecuteAndAwaitOperation_WhenOperationFailsWithoutErrorDetails()
        {
            var request = new Mock<IClientServiceRequest<Operation>>();
            request
                .Setup(r => r.ExecuteAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Operation()
                {
                    Status = "DONE",
                    HttpErrorStatusCode = 412,
                    HttpErrorMessage = "MockError",
                });

            var e = await ExceptionAssert
                .ThrowsAsync<GoogleApiException>(() => request.Object
                    .ExecuteAndAwaitOperationAsync("project-1", CancellationToken.None))
                    .ConfigureAwait(false);

            Assert.That(e.Message, Is.EqualTo("MockError"));
            Assert.That(e.Error.Message, Is.EqualTo("MockError"));
            CollectionAssert.IsEmpty(e.Error.Errors);
        }

        [Test]
        public async Task ExecuteAndAwaitOperation_WhenOperationFailsWithErrorDetails_ThenThrowsExceptionWithReason()
        {
            var request = new Mock<IClientServiceRequest<Operation>>();
            request
                .Setup(r => r.ExecuteAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Operation()
                {
                    Status = "DONE",
                    HttpErrorStatusCode = 412,
                    HttpErrorMessage = "MockError",
                    Error = new Operation.ErrorData()
                    {
                        Errors = new[]
                        {
                            new Operation.ErrorData.ErrorsData()
                            {
                               Code = "CONDITION_NOT_MET",
                               Message = "message"
                            }
                        }
                    }
                });

            var e = await ExceptionAssert
                .ThrowsAsync<GoogleApiException>(() => request.Object
                    .ExecuteAndAwaitOperationAsync("project-1", CancellationToken.None))
                .ConfigureAwait(false);

            Assert.That(e.Message, Is.EqualTo("MockError"));
            Assert.That(e.Error.Message, Is.EqualTo("MockError"));
            Assert.That((int)e.Error.Code, Is.EqualTo(412));
            Assert.That(e.Error.Errors.First().Reason, Is.EqualTo("message"));
        }
    }
}