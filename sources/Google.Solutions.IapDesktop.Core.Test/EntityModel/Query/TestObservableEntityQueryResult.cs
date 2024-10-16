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

using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Linq;
using Google.Solutions.IapDesktop.Core.EntityModel;
using Google.Solutions.IapDesktop.Core.EntityModel.Query;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Moq;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Core.Test.EntityModel.Query
{
    [TestFixture]
    public class TestObservableEntityQueryResult
    {
        //----------------------------------------------------------------------
        // EntityRemovedEvent.
        //----------------------------------------------------------------------

        [Test]
        public void EntityRemovedEvent_RemovesItem()
        {
            // Capture event handler that the result will register.
            Action<EntityRemovedEvent>? eventHandler = null;
            var eventQueue = new Mock<IEventQueue>();
            eventQueue
                .Setup(e => e.Subscribe(
                    It.IsAny<Action<EntityRemovedEvent>>(),
                    SubscriptionOptions.WeakSubscriberReference))
                .Callback<Action<EntityRemovedEvent>, SubscriptionOptions>((e, _) => eventHandler = e);

            var result = new ObservableEntityQueryResult<EntityType>(
                Lists.FromNullable(new EntityQueryResultItem<EntityType>(new EntityType(typeof(string)))),
                eventQueue.Object);

            Assert.IsNotNull(eventHandler);
            Assert.AreEqual(1, result.Count);

            // Irrelevant event.
            eventHandler!(new EntityRemovedEvent(new EntityTypeLocator(typeof(int))));
            Assert.AreEqual(1, result.Count);

            // Matching event.
            eventHandler!(new EntityRemovedEvent(new EntityTypeLocator(typeof(string))));
            Assert.AreEqual(0, result.Count);
        }

        //----------------------------------------------------------------------
        // EntityPropertyChangedEvent.
        //----------------------------------------------------------------------

        [Test]
        public void EntityPropertyChangedEvent_NotifiesEntity()
        {
            // Capture event handler that the result will register.
            Action<EntityPropertyChangedEvent>? eventHandler = null;
            var eventQueue = new Mock<IEventQueue>();
            eventQueue
                .Setup(e => e.Subscribe(
                    It.IsAny<Action<EntityPropertyChangedEvent>>(),
                    SubscriptionOptions.WeakSubscriberReference))
                .Callback<Action<EntityPropertyChangedEvent>, SubscriptionOptions>((e, _) => eventHandler = e);

            var entity = new EntityType(typeof(string));
            var subscriberAspect = new Mock<ISubscriber<EntityPropertyChangedEvent>>();

            var result = new ObservableEntityQueryResult<EntityType>(
                Lists.FromNullable(new EntityQueryResultItem<EntityType>(
                    entity,
                    new System.Collections.Generic.Dictionary<Type, object?>
                    {
                        { typeof(ISubscriber<EntityPropertyChangedEvent>), subscriberAspect.Object }
                    })),
                eventQueue.Object);

            Assert.IsNotNull(eventHandler);
            Assert.AreEqual(1, result.Count);

            // Irrelevant event.
            eventHandler!(new EntityPropertyChangedEvent(
                new ProjectLocator("project-1"), typeof(EntityType), "SomeProperty"));

            // Matching event.
            var ev = new EntityPropertyChangedEvent(entity.Locator, typeof(EntityType), "SomeProperty");
            eventHandler!(ev);

            subscriberAspect.Verify(a => a.Notify(ev), Times.Once);
        }
    }
}
