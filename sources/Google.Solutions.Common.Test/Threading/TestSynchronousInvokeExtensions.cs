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

using Google.Solutions.Common.Threading;
using Google.Solutions.Testing.Apis;
using Moq;
using NUnit.Framework;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Test.Threading
{
    [TestFixture]
    public class TestSynchronousInvokeExtensions
    {
        private class Invoker : ISynchronizeInvoke
        {
            public bool InvokeRequired { get; set; }

            public IAsyncResult BeginInvoke(Delegate method, object[] args)
            {
                if (this.InvokeRequired)
                {
                    Task.Run(() => method.DynamicInvoke(args));
                }
                else
                {
                    method.DynamicInvoke(args);
                }

                return new Mock<IAsyncResult>().Object;
            }

            public object? EndInvoke(IAsyncResult result)
            {
                return null;
            }

            public object Invoke(Delegate method, object[] args)
            {
                throw new NotImplementedException();
            }
        }

        //---------------------------------------------------------------------
        // Invoke.
        //---------------------------------------------------------------------

        [Test]
        public void Invoke_WhenInvokeNotRequired_ThenInvokeAsyncWorksSynchronously()
        {
            var invoker = new Invoker()
            {
                InvokeRequired = false
            };

            var invoked = false;
            var task = invoker.InvokeAsync(() =>
            {
                invoked = true;
                return Task.CompletedTask;
            });

            Assert.IsTrue(invoked);
        }

        [Test]
        public async Task Invoke_WhenInvokeRequired_ThenInvokeAsyncWorksAsynchronouslyAndPropagatesResult()
        {
            var invoker = new Invoker()
            {
                InvokeRequired = true
            };

            var tcs = new TaskCompletionSource<object?>();

            var invoked = false;
            var task = invoker.InvokeAsync(async () =>
            {
                await tcs.Task.ConfigureAwait(false);
                invoked = true;
            });

            Assert.IsFalse(invoked);
            tcs.SetResult(null);

            await task.ConfigureAwait(false);
            Assert.IsTrue(invoked);
        }

        [Test]
        public async Task Invoke_WhenInvokeRequired_ThenInvokeAsyncWorksAsynchronouslyAndPropagatesException()
        {
            var invoker = new Invoker()
            {
                InvokeRequired = true
            };

            var tcs = new TaskCompletionSource<object?>();

            var invoked = false;
            var task = invoker.InvokeAsync(async () =>
            {
                await tcs.Task.ConfigureAwait(false);
                invoked = true;
                throw new ArgumentException("test");
            });

            Assert.IsFalse(invoked);
            tcs.SetResult(null);

            await ExceptionAssert
                .ThrowsAsync<ArgumentException>(() => task)
                .ConfigureAwait(false);
            Assert.IsTrue(invoked);
        }
    }
}
