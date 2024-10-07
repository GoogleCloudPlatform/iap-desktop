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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.EntityModel.Query
{
    /// <summary>
    /// Result of an entity query.
    /// </summary>
    public sealed class EntityQueryResult<TEntity> 
        : ReadOnlyCollection<EntityQueryResult<TEntity>.Item>, IDisposable
        where TEntity : IEntity<ILocator>
    {
        public EntityQueryResult(IList<Item> list) : base(list)
        {
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// An entity with associated aspects.
        /// </summary>
        public class Item
        {
            private readonly Dictionary<Type, object?> aspects;
            private readonly Dictionary<Type, DeriveAspectDelegate> derivedAspects;

            internal static async Task<Item> CreateAsync(
                TEntity entity,
                Dictionary<Type, Task<object?>> aspectTasks,
                Dictionary<Type, DeriveAspectDelegate> derivedAspects)
            {
                await Task
                    .WhenAll(aspectTasks.Values)
                    .ConfigureAwait(false);

                return new Item(
                    entity,
                    aspectTasks.ToDictionary(
                        item => item.Key,
                        item => item.Value.Result),
                    derivedAspects);
            }

            internal Item(
                TEntity entity,
                Dictionary<Type, object?> aspectValues,
                Dictionary<Type, DeriveAspectDelegate> derivedAspects)
            {
                this.Entity = entity;
                this.aspects = aspectValues;
                this.derivedAspects = derivedAspects;
            }

            internal Item(
                TEntity entity,
                Dictionary<Type, object?> aspectValues)
                : this(entity, aspectValues, new Dictionary<Type, DeriveAspectDelegate>())
            {
            }

            /// <summary>
            /// The entity itself.
            /// </summary>
            public TEntity Entity { get; }

            /// <summary>
            /// Get aspect for this entity.
            /// </summary>
            public TAspect? Aspect<TAspect>() where TAspect : class
            {
                if (this.aspects.ContainsKey(typeof(TAspect)))
                {
                    return this.aspects[typeof(TAspect)] as TAspect;
                }
                else if (this.derivedAspects.ContainsKey(typeof(TAspect)))
                {
                    return this.derivedAspects[typeof(TAspect)](this.aspects) as TAspect;
                }
                else
                {
                    throw new ArgumentException(
                        $"The query does not include aspect '${typeof(TAspect)}'");
                }
            }
        }
    }
}
