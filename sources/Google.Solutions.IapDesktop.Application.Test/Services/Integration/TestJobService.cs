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

using Google.Apis.Auth.OAuth2.Responses;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.Testing.Application.Test;
using Google.Solutions.Testing.Common;
using Moq;
using NUnit.Framework;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Integration
{
    [TestFixture]
    public class TestJobService : ApplicationFixtureBase
    {
        private class SynchronousInvoker : ISynchronizeInvoke
        {
            public bool InvokeRequired => false;

            public IAsyncResult BeginInvoke(Delegate method, object[] args)
            {
                method.DynamicInvoke(args);
                return null;
            }

            public object EndInvoke(IAsyncResult result)
            {
                throw new NotImplementedException();
            }

            public object Invoke(Delegate method, object[] args)
            {
                method.DynamicInvoke(args);
                return null;
            }
        }

        private class UserFeedback : IJobUserFeedback
        {
            public bool IsShowing => true;

            public void Finish()
            {
            }

            public void Start()
            {
            }
        }

        private Mock<IAuthorizationSource> authService = null;
        private Mock<IJobHost> jobHost = null;
        private JobService jobService = null;

        [SetUp]
        public void SetUp()
        {
            var authz = new Mock<IAuthorization>();

            this.authService = new Mock<IAuthorizationSource>();
            this.authService.SetupGet(a => a.Authorization).Returns(authz.Object);
            this.authService.Setup(a => a.ReauthorizeAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true));

            var invoker = new SynchronousInvoker();

            this.jobHost = new Mock<IJobHost>();
            this.jobHost.SetupGet(h => h.Invoker).Returns(invoker);
            this.jobHost.Setup(h => h.ShowFeedback(
                It.IsNotNull<JobDescription>(),
                It.IsNotNull<CancellationTokenSource>())).Returns(new UserFeedback());

            this.jobService = new JobService(authService.Object, this.jobHost.Object);
        }

        [Test]
        public async Task WhenReauthRequired_ThenReauthConfirmationIsPrompted()
        {
            this.jobHost.Setup(h => h.ConfirmReauthorization()).Returns(true);

            int funcCall = 0;
            var result = await this.jobService
                .RunInBackground<string>(
                    new JobDescription("test"),
                    token =>
                    {
                        if (funcCall++ == 0)
                        {
                            throw new TokenResponseException(
                                new TokenErrorResponse()
                                {
                                    Error = "invalid_grant"
                                });
                        }
                        else
                        {
                            return Task.FromResult("data");
                        }
                    })
                .ConfigureAwait(true);

            Assert.AreEqual("data", result);

            this.jobHost.Verify(h => h.ConfirmReauthorization(), Times.Once);
        }

        [Test]
        public async Task WhenReauthConfirmed_ThenFuncIsRepeated()
        {
            this.jobHost.Setup(h => h.ConfirmReauthorization()).Returns(true);

            int funcCall = 0;
            var result = await this.jobService
                .RunInBackground<string>(
                    new JobDescription("test"),
                    token =>
                    {
                        if (funcCall++ == 0)
                        {
                            throw new TokenResponseException(
                                new TokenErrorResponse()
                                {
                                    Error = "invalid_grant"
                                });
                        }
                        else
                        {
                            return Task.FromResult("data");
                        }
                    })
                .ConfigureAwait(true);

            Assert.AreEqual("data", result);
            Assert.AreEqual(2, funcCall);
        }

        [Test]
        public void WhenReauthDenied_ThenTaskCanceledExceptionIsPopagated()
        {
            this.jobHost.Setup(h => h.ConfirmReauthorization()).Returns(false);

            ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(() =>
            {
                this.jobService.RunInBackground<string>(
                    new JobDescription("test"),
                    token =>
                    {
                        throw new TokenResponseException(
                            new TokenErrorResponse()
                            {
                                Error = "invalid_grant"
                            });
                    }).Wait();
            });

            this.jobHost.Verify(h => h.ConfirmReauthorization(), Times.Once);
        }

        [Test]
        public void WhenReauthFailed_ThenExceptionIsPropagated()
        {
            this.jobHost.Setup(h => h.ConfirmReauthorization()).Returns(true);
            this.authService.Setup(a => a.ReauthorizeAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(new ApplicationException()));

            ExceptionAssert.ThrowsAggregateException<ApplicationException>(() =>
            {
                this.jobService.RunInBackground<string>(
                    new JobDescription("test"),
                    token =>
                    {
                        throw new TokenResponseException(
                            new TokenErrorResponse()
                            {
                                Error = "invalid_grant"
                            });
                    }).Wait();
            });
        }





        [Test]
        public async Task WhenReauthRequired_ThenReauthConfirmationIsPrompted_WithAggregateException()
        {
            this.jobHost.Setup(h => h.ConfirmReauthorization()).Returns(true);

            int funcCall = 0;
            var result = await this.jobService
                .RunInBackground<string>(
                    new JobDescription("test"),
                    token =>
                    {
                        if (funcCall++ == 0)
                        {
                            throw new AggregateException(
                                new TokenResponseException(
                                    new TokenErrorResponse()
                                    {
                                        Error = "invalid_grant"
                                    }));
                        }
                        else
                        {
                            return Task.FromResult("data");
                        }
                    })
                .ConfigureAwait(true);

            Assert.AreEqual("data", result);

            this.jobHost.Verify(h => h.ConfirmReauthorization(), Times.Once);
        }

        [Test]
        public async Task WhenReauthConfirmed_ThenFuncIsRepeated_WithAggregateException()
        {
            this.jobHost.Setup(h => h.ConfirmReauthorization()).Returns(true);

            int funcCall = 0;
            var result = await this.jobService
                .RunInBackground<string>(
                    new JobDescription("test"),
                    token =>
                    {
                        if (funcCall++ == 0)
                        {
                            throw new AggregateException(
                                new TokenResponseException(
                                    new TokenErrorResponse()
                                    {
                                        Error = "invalid_grant"
                                    }));
                        }
                        else
                        {
                            return Task.FromResult("data");
                        }
                    })
                .ConfigureAwait(true);

            Assert.AreEqual("data", result);
            Assert.AreEqual(2, funcCall);
        }

        [Test]
        public void WhenReauthDenied_ThenTaskCanceledExceptionIsPopagated_WithAggregateException()
        {
            this.jobHost.Setup(h => h.ConfirmReauthorization()).Returns(false);

            ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(() =>
            {
                this.jobService.RunInBackground<string>(
                    new JobDescription("test"),
                    token =>
                    {
                        throw new AggregateException(
                            new TokenResponseException(
                                new TokenErrorResponse()
                                {
                                    Error = "invalid_grant"
                                }));
                    }).Wait();
            });

            this.jobHost.Verify(h => h.ConfirmReauthorization(), Times.Once);
        }

        [Test]
        public void WhenReauthFailed_ThenExceptionIsPropagated_WithAggregateException()
        {
            this.jobHost.Setup(h => h.ConfirmReauthorization()).Returns(true);
            this.authService.Setup(a => a.ReauthorizeAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(new ApplicationException()));

            ExceptionAssert.ThrowsAggregateException<ApplicationException>(() =>
            {
                this.jobService.RunInBackground<string>(
                    new JobDescription("test"),
                    token =>
                    {
                        throw new AggregateException(
                            new TokenResponseException(
                                new TokenErrorResponse()
                                {
                                    Error = "invalid_grant"
                                }));
                    }).Wait();
            });
        }
    }
}
