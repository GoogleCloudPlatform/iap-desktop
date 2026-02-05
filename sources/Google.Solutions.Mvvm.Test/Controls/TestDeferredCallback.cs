//
// Copyright 2024 Google LLC
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

using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Testing.Apis;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Mvvm.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestDeferredCallback
    {
        [WindowsFormsTest]
        public async Task Schedule_MultipleInvocationsAreCoalesced()
        {
            var invocations = 0;
            DeferredCallback? callback = null;
            using (callback = new DeferredCallback(
                cb =>
                {
                    Assert.IsNotNull(callback);
                    Assert.That(callback!.IsPending, Is.False);
                    invocations++;
                },
                TimeSpan.FromMilliseconds(10)))
            {
                callback.Schedule();
                callback.Schedule();
                callback.Schedule();

                var ctx = SynchronizationContext.Current;
                await callback
                    .WaitForCompletionAsync()
                    .ConfigureAwait(false);

                Assert.That(invocations, Is.EqualTo(1));
            }
        }

        [WindowsFormsTest]
        public async Task Schedule_WhenCallbackDefersItself()
        {
            var invocations = 0;
            using (var callback = new DeferredCallback(
                cb =>
                {
                    invocations++;

                    if (invocations < 2)
                    {
                        cb.Defer();
                    }
                },
                TimeSpan.FromMilliseconds(10)))
            {
                callback.Schedule();

                await callback
                    .WaitForCompletionAsync()
                    .ConfigureAwait(true);

                Assert.That(invocations, Is.EqualTo(2));
            }
        }

        [WindowsFormsTest]
        public async Task Schedule_WhenNoCallbackScheduled()
        {
            var callback = new DeferredCallback(_ => { }, TimeSpan.FromSeconds(1));
            await callback
                .WaitForCompletionAsync()
                .ConfigureAwait(true);
        }
    }
}
