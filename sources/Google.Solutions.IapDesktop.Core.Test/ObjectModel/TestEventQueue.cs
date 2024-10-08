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

using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Testing.Apis;
using Moq;
using NUnit.Framework;
using System;
using System.ComponentModel;
using System.Linq;

namespace Google.Solutions.IapDesktop.Core.Test.ObjectModel
{
    [TestFixture]
    public class TestEventQueue
    {
        private class EventOne { }
        private class EventTwo { }

        //---------------------------------------------------------------------
        // Subscribe.
        //---------------------------------------------------------------------

        [Test]
        public void Subscribe_StrongSubscriberReference()
        {
            var queue = new EventQueue(new Mock<ISynchronizeInvoke>().Object);
            using (queue.Subscribe<EventOne>(e => { }, SubscriptionOptions.None))
            {
                var sub = queue.GetSubscriptions<EventOne>().First();
                Assert.IsInstanceOf<EventQueue.StrongSubscription<EventOne>>(sub);
            }
        }

        [Test]
        public void Subscribe_WeakSubscriberReference()
        {
            var queue = new EventQueue(new Mock<ISynchronizeInvoke>().Object);
            using (queue.Subscribe<EventOne>(e => { }, SubscriptionOptions.WeakSubscriberReference))
            {
                var sub = queue.GetSubscriptions<EventOne>().First();
                Assert.IsInstanceOf<EventQueue.WeakSubscription<EventOne>>(sub);
            }
        }

        [Test]
        public void Subscribe(
            [Values(SubscriptionOptions.None, SubscriptionOptions.WeakSubscriberReference)]
            SubscriptionOptions options)
        {
            var queue = new EventQueue(new Mock<ISynchronizeInvoke>().Object);
            using (queue.Subscribe<EventOne>(e => { }, options))
            using (queue.Subscribe<EventOne>(e => { }, options))
            {
                Assert.AreEqual(2, queue.GetSubscriptions<EventOne>().Count());
                Assert.AreEqual(0, queue.GetSubscriptions<EventTwo>().Count());
            }
        }

        //---------------------------------------------------------------------
        // Unsubscribe.
        //---------------------------------------------------------------------

        [Test]
        public void Unsubscribe(
            [Values(SubscriptionOptions.None, SubscriptionOptions.WeakSubscriberReference)]
            SubscriptionOptions options)
        {
            var queue = new EventQueue(new Mock<ISynchronizeInvoke>().Object);
            using (queue.Subscribe<EventOne>(e => { }, options))
            {
                Assert.AreEqual(1, queue.GetSubscriptions<EventOne>().Count());
            }

            Assert.AreEqual(0, queue.GetSubscriptions<EventOne>().Count());
        }

        [Test]
        public void Unsubscribe_WhenSubscriptionDisposed(
            [Values(SubscriptionOptions.None, SubscriptionOptions.WeakSubscriberReference)]
            SubscriptionOptions options)
        {
            var queue = new EventQueue(new Mock<ISynchronizeInvoke>().Object);

            var invoked = false;
            var sub = (EventQueue.Subscription<EventOne>)
                queue.Subscribe<EventOne>(e => { invoked = true; }, options);
            sub.Dispose();

            sub.InvokeAsync(new EventOne());
            Assert.IsFalse(invoked);
        }

        //---------------------------------------------------------------------
        // PublishAsync.
        //---------------------------------------------------------------------

        [Test]
        public void PublishAsync_WhenInvokeNotRequired()
        {
            var invoker = new Mock<ISynchronizeInvoke>();
            invoker.SetupGet(i => i.InvokeRequired).Returns(false);

            var queue = new EventQueue(invoker.Object);

            var invoked = false;
            using (queue.Subscribe<EventOne>(e => { invoked = true; }))
            {
                var t = queue.PublishAsync(new EventOne());
                Assert.IsTrue(invoked);
            }
        }

        [Test]
        public void PublishAsync_WhenSubscriberThrowsException()
        {
            var invoker = new Mock<ISynchronizeInvoke>();
            invoker.SetupGet(i => i.InvokeRequired).Returns(false);

            var queue = new EventQueue(invoker.Object);
            using (queue.Subscribe<EventOne>(_ => throw new ApplicationException("mock")))
            {
                ExceptionAssert.ThrowsAggregateException<ApplicationException>(
                    () => queue.PublishAsync(new EventOne()).Wait());
            }
        }

        [Test]
        public void PublishAsync_WhenWeakSubscriberWasGarbageCollected()
        {
            var invoker = new Mock<ISynchronizeInvoke>();
            invoker.SetupGet(i => i.InvokeRequired).Returns(false);

            var queue = new EventQueue(invoker.Object);

            var invoked = false;
            using (queue.Subscribe<EventOne>(
                e => { invoked = true; }, 
                SubscriptionOptions.WeakSubscriberReference))
            {
                queue.GetSubscriptions<EventOne>()
                    .Cast<EventQueue.WeakSubscription<EventOne>>()
                    .First()
                    .SimulateSubscriberWasGarbageCollected();

                var t = queue.PublishAsync(new EventOne());

                // Susbcription auto-removed.
                Assert.AreEqual(0, queue.GetSubscriptions<EventOne>().Count());
                Assert.IsFalse(invoked);
            }
        }

        //---------------------------------------------------------------------
        // Publish.
        //---------------------------------------------------------------------

        [Test]
        public void Publish_WhenSubscriberThrowsException()
        {
            var invoker = new Mock<ISynchronizeInvoke>();
            invoker.SetupGet(i => i.InvokeRequired).Returns(false);

            var queue = new EventQueue(invoker.Object);
            using (queue.Subscribe<EventOne>(_ => throw new ApplicationException("mock")))
            {
                queue.Publish(new EventOne());
            }
        }
    }
}
