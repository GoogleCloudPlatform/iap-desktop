//
// Copyright 2022 Google LLC
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
using NUnit.Framework;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Test.Threading
{
    [TestFixture]
    public class TestSingleThreadSynchronizationContext : CommonFixtureBase
    {
        //---------------------------------------------------------------------
        // Post.
        //---------------------------------------------------------------------

        [Test]
        public void WhenPostingCallback_ThenCallbackIsInvokedOnDesignatedThread()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                var ctx = new SingleThreadSynchronizationContext();
                var thread = new Thread(_ =>
                {
                    ctx.Pump(tokenSource.Token);
                });

                thread.Start();

                void callback(object state)
                {
                    Assert.AreEqual("test", state);
                    Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, thread.ManagedThreadId);

                    tokenSource.Cancel();
                }

                ctx.Post(callback, "test");

                thread.Join();
            }
        }

        //---------------------------------------------------------------------
        // Pump.
        //---------------------------------------------------------------------

        [Test]
        public void Pump_WhenCalledOnWrongThread_ThenPumpThrowsException()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                tokenSource.Cancel();

                var ctx = new SingleThreadSynchronizationContext();

                ctx.Pump(tokenSource.Token);
                var thread = new Thread(_ =>
                {
                    Assert.Throws<InvalidOperationException>(
                        () => ctx.Pump(tokenSource.Token));
                });

                thread.Start();
                thread.Join();
            }
        }

        [Test]
        public void Pump_WhenCallbackThrowsException_ThenPumpThrowsException()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                var ctx = new SingleThreadSynchronizationContext();
                var thread = new Thread(_ =>
                {
                    Assert.Throws<TargetInvocationException>(
                        () => ctx.Pump(tokenSource.Token));
                });

                thread.Start();

                ctx.Post(_ => throw new ArgumentException(), null!);

                thread.Join();
            }
        }

        //---------------------------------------------------------------------
        // Run.
        //---------------------------------------------------------------------

        [Test]
        public async Task Run_WhenPostingCallbackUsingRunAsync_ThenCallbackIsInvokedOnDesignatedThread(
            [Values(true, false)] bool continueOnCapturedContext)
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                var ctx = new SingleThreadSynchronizationContext();
                var thread = new Thread(_ =>
                {
                    ctx.Pump(tokenSource.Token);
                });

                thread.Start();

                await ctx.RunAsync(() =>
                    {
                        Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, thread.ManagedThreadId);

                        tokenSource.Cancel();
                    })
                    .ConfigureAwait(continueOnCapturedContext);

                thread.Join();
            }
        }
    }
}
