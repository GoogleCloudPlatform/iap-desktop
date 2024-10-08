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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.EntityModel.Query
{
    /// <summary>
    /// Delegate for deriving an aspect from one or more input aspects.
    /// </summary>
    internal delegate object? DeriveAspectDelegate(IDictionary<Type, object?> aspectValues);

    /// <summary>
    /// Delegate for looking up entities.
    /// </summary>
    internal delegate Task<ICollection<TEntity>> QueryDelegate<TEntity>(CancellationToken cancellationToken)
        where TEntity : IEntity<ILocator>;

    /// <summary>
    /// Queries an entity context using idiomatic syntax.
    /// </summary>
    public class EntityQuery<TEntity>
        where TEntity : class, IEntity<ILocator>
    {
        private readonly EntityContext context;
        private readonly QueryDelegate<TEntity> query;
        private readonly Dictionary<Type, Func<ILocator, CancellationToken, Task<object?>>> aspects
            = new Dictionary<Type, Func<ILocator, CancellationToken, Task<object?>>>();
        private readonly Dictionary<Type, DeriveAspectDelegate> derivedAspects
            = new Dictionary<Type, DeriveAspectDelegate>();

        private EntityQuery(
            EntityContext context,
            QueryDelegate<TEntity> query)
        {
            this.context = context;
            this.query = query;
        }

        private async Task<object?> QueryAspectAsync<TAspect>(
            ILocator locator,
            CancellationToken cancellationToken)
            where TAspect : class
        {
            return await this.context
                .QueryAspectAsync<TAspect>(locator, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<IList<EntityQueryResultItem<TEntity>>> ExecuteCoreAsync(
            CancellationToken cancellationToken)
        {
            //
            // Query entities.
            //
            var entities = await this
                .query(cancellationToken)
                .ConfigureAwait(false);

            //
            // Kick of asynchronous queries for each aspects and entity.
            //
            return await Task.WhenAll(entities
                .EnsureNotNull()
                .Select(e => EntityQueryResultItem<TEntity>.CreateAsync(
                    e,
                    this.aspects.ToDictionary(
                        item => item.Key,
                        item => item.Value(e.Locator, cancellationToken)),
                    this.derivedAspects))
                .ToList());
        }

        /// <summary>
        /// Include an aspect in the query so that its value
        /// can later be accessed in EntityWithAspects.
        /// </summary>
        public EntityQuery<TEntity> IncludeAspect<TAspect>()
            where TAspect : class
        {
            //
            // Record that we need to query this aspect. We store a 
            // delegate (as opposed to just the aspect type) because
            // we won't have access to type information later.
            //
            if (!this.aspects.ContainsKey(typeof(TAspect)))
            {
                this.aspects.Add(typeof(TAspect), QueryAspectAsync<TAspect>);
            }

            return this;
        }

        /// <summary>
        /// Derive an aspect from another aspect.
        /// </summary>
        public EntityQuery<TEntity> IncludeAspect<TInputAspect, TAspect>(
            Func<TInputAspect?, TAspect?> func)
            where TInputAspect : class
            where TAspect : class
        {
            //
            // Make sure we query the input aspect.
            //
            IncludeAspect<TInputAspect>();

            this.derivedAspects[typeof(TAspect)] = values =>
            {
                TInputAspect? input = null;
                if (values.TryGetValue(typeof(TInputAspect), out var value))
                {
                    input = value as TInputAspect;
                }

                return func(input);
            };

            return this;
        }

        /// <summary>
        /// Execute query.
        /// </summary>
        /// <returns>
        /// Snapshot of entities and their aspects.
        /// </returns>
        public async Task<EntityQueryResult<TEntity>> ExecuteAsync(
            CancellationToken cancellationToken)
        {
            return new EntityQueryResult<TEntity>(
                await ExecuteCoreAsync(cancellationToken).ConfigureAwait(false));
        }

        /// <summary>
        /// Builder class for queries.
        /// </summary>
        public class Builder
        {
            private readonly EntityContext context;

            internal Builder(EntityContext context)
            {
                this.context = context;
            }

            /// <summary>
            /// Query entities by performing a search.
            /// </summary>
            public EntityQuery<TEntity> Search<TQuery>(TQuery query)
            {
                return new EntityQuery<TEntity>(
                    this.context,
                    ct => this.context.SearchAsync<TQuery, TEntity>(query, ct));
            }

            /// <summary>
            /// Query entities by performing a wildcard search.
            /// </summary>
            public EntityQuery<TEntity> List()
            {
                return Search(WildcardQuery.Instance);
            }

            /// <summary>
            /// Query a single entity.
            /// </summary>
            public EntityQuery<TEntity> Get(ILocator locator)
            {
                return new EntityQuery<TEntity>(
                    this.context,
                    async ct => Lists.FromNullable(await this.context
                        .QueryAspectAsync<TEntity>(locator, ct)
                        .ConfigureAwait(false)));
            }

            /// <summary>
            /// Query entities by parent locator.
            /// </summary>
            /// <param name="locator">Locator of parent entity</param>
            public EntityQuery<TEntity> ByAncestor(ILocator locator)
            {
                return new EntityQuery<TEntity>(
                    this.context,
                    ct => this.context.ListDescendantsAsync<TEntity>(locator, ct));
            }
        }
    }
}