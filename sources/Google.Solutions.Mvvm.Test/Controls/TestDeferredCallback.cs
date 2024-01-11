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
using NUnit.Framework;
using System;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestDeferredCallback
    {
        [Test]
        public void MultipleInvocationsAreCoalesced()
        {
            int invocations = 0;
            using (var callback = new DeferredCallback(
                cb =>
                {
                    Assert.IsFalse(cb.IsCallbackPending);
                    invocations++;
                },
                TimeSpan.FromMilliseconds(10)))
            {
                callback.Invoke();
                callback.Invoke();
                callback.Invoke();

                for (int i = 0; callback.IsCallbackPending && i < 50; i++)
                {
                    Thread.Sleep(1);
                    Application.DoEvents();
                }

                Assert.AreEqual(1, invocations);
            }
        }

        [Test]
        public void CallbackCanDeferItself()
        {
            int invocations = 0;
            using (var callback = new DeferredCallback(
                cb =>
                {
                    invocations++;

                    if (invocations < 2)
                    {
                        cb.Invoke();
                    }
                },
                TimeSpan.FromMilliseconds(10)))
            {
                callback.Invoke();
                callback.Invoke();
                callback.Invoke();

                for (int i = 0; callback.IsCallbackPending && i < 50; i++)
                {
                    Thread.Sleep(1);
                    Application.DoEvents();
                }

                Assert.AreEqual(2, invocations);
            }
        }
    }
}
