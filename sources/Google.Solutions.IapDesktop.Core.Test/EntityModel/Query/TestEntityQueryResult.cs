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

using Google.Solutions.IapDesktop.Core.EntityModel;
using Google.Solutions.IapDesktop.Core.EntityModel.Query;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Core.Test.EntityModel.Query
{
    [TestFixture]
    public class TestEntityQueryResult
    {
        [Test]
        public void IsReadOnly()
        {
            var result = (ICollection<EntityQueryResult<EntityType>.Item>) 
                new EntityQueryResult<EntityType>(Array.Empty<EntityQueryResult<EntityType>.Item>());
            Assert.IsTrue(result.IsReadOnly);
        }

        [TestFixture]
        public class Item
        {
            //------------------------------------------------------------------
            // Entity.
            //------------------------------------------------------------------

            [Test]
            public void Entity()
            {
                var entity = new EntityType(typeof(string));
                var item = new EntityQueryResult<EntityType>.Item(
                    entity,
                    new Dictionary<Type, object?>());

                Assert.AreSame(entity, item.Entity);
            }

            //------------------------------------------------------------------
            // Aspect.
            //------------------------------------------------------------------

            [Test]
            public void Aspect_WhenNull()
            {
                var aspects = new Dictionary<Type, object?>()
                { 
                    { typeof(string), null }
                };

                var item = new EntityQueryResult<EntityType>.Item(
                    new EntityType(typeof(string)),
                    aspects);

                Assert.IsNull(item.Aspect<string>());
            }

            [Test]
            public void Aspect_WhenNotNull()
            {
                var aspects = new Dictionary<Type, object?>()
                {
                    { typeof(string), "test" }
                };

                var item = new EntityQueryResult<EntityType>.Item(
                    new EntityType(typeof(string)),
                    aspects);

                Assert.AreEqual("test", item.Aspect<string>());
            }

            [Test]
            public void Aspect_WhenNotIncluded()
            {
                var item = new EntityQueryResult<EntityType>.Item(
                    new EntityType(typeof(string)),
                    new Dictionary<Type, object?>());

                Assert.Throws<ArgumentException>(() => item.Aspect<string>());
            }

            //------------------------------------------------------------------
            // Aspect - derived.
            //------------------------------------------------------------------

            [Test]
            public void Aspect_WhenDerived_ThenResultIsMemoized()
            {
                var aspects = new Dictionary<Type, object?>()
                {
                    { typeof(Version), new Version(1, 2) }
                };

                var deriveCalls = 0;
                var derivedAspects = new Dictionary<Type, DeriveAspectDelegate>()
                {
                    { typeof(string), aspects =>
                        {
                            deriveCalls++;
                            return aspects[typeof(Version)]?.ToString();
                        }
                    }
                };

                var item = new EntityQueryResult<EntityType>.Item(
                    new EntityType(typeof(string)),
                    aspects, 
                    derivedAspects);

                Assert.AreEqual("1.2", item.Aspect<string>()); // invocation #1
                Assert.AreEqual("1.2", item.Aspect<string>()); // invocation #2
                Assert.AreEqual(1, deriveCalls);
            }
        }
    }
}
