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
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Application.Test;
using Moq;
using NUnit.Framework;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Windows
{
    [TestFixture]
    public class TestJobService : ApplicationFixtureBase
    {
        private class SynchronousInvoker : ISynchronizeInvoke
        {
            public bool InvokeRequired => false;

            public IAsyncResult? BeginInvoke(Delegate method, object[] args)
            {
                method.DynamicInvoke(args);
                return null;
            }

            public object EndInvoke(IAsyncResult result)
            {
                throw new NotImplementedException();
            }

            public object? Invoke(Delegate method, object[] args)
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

        private static Mock<IJobHost> CreateJobHost()
        {
            var jobHost = new Mock<IJobHost>();
            jobHost.SetupGet(h => h.Invoker).Returns(new SynchronousInvoker());
            jobHost.Setup(h => h.ShowFeedback(
                It.IsNotNull<JobDescription>(),
                It.IsNotNull<CancellationTokenSource>())).Returns(new UserFeedback());

            return jobHost;
        }

        [Test]
        public async Task WhenReauthRequired_ThenReauthConfirmationIsPrompted()
        {
            var jobHost = CreateJobHost();
            var jobService = new JobService(jobHost.Object);

            var funcCall = 0;
            var result = await jobService
                .RunAsync(
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

            jobHost.Verify(h => h.Reauthorize(), Times.Once);
        }

        [Test]
        public async Task WhenReauthConfirmed_ThenFuncIsRepeated()
        {
            var jobHost = CreateJobHost();
            var jobService = new JobService(jobHost.Object);

            var funcCall = 0;
            var result = await jobService
                .RunAsync(
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
        public void WhenReauthCancelled_ThenTaskCanceledExceptionIsPopagated()
        {
            var jobHost = CreateJobHost();
            var jobService = new JobService(jobHost.Object);

            jobHost
                .Setup(h => h.Reauthorize())
                .Throws(new TaskCanceledException("reauth aborted"));

            ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(() =>
            {
                jobService.RunAsync<string>(
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

            jobHost.Verify(h => h.Reauthorize(), Times.Once);
        }

        [Test]
        public async Task WhenReauthRequired_ThenReauthConfirmationIsPrompted_WithAggregateException()
        {
            var jobHost = CreateJobHost();
            var jobService = new JobService(jobHost.Object);

            var funcCall = 0;
            var result = await jobService
                .RunAsync(
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

            jobHost.Verify(h => h.Reauthorize(), Times.Once);
        }

        [Test]
        public async Task WhenReauthConfirmed_ThenFuncIsRepeated_WithAggregateException()
        {
            var jobHost = CreateJobHost();
            var jobService = new JobService(jobHost.Object);

            var funcCall = 0;
            var result = await jobService
                .RunAsync(
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
        public void WhenReauthCancelled_ThenTaskCanceledExceptionIsPopagated_WithAggregateException()
        {
            var jobHost = CreateJobHost();
            var jobService = new JobService(jobHost.Object);

            jobHost
                .Setup(h => h.Reauthorize())
                .Throws(new TaskCanceledException("reauth aborted"));

            ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(() =>
            {
                jobService.RunAsync<string>(
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

            jobHost.Verify(h => h.Reauthorize(), Times.Once);
        }
    }
}
