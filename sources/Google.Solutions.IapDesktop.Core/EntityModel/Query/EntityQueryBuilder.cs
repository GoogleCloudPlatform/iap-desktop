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
using Google.Solutions.Common.Util;
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
    /// Allows idiomatic querying of an entity context.
    /// </summary>
    public class EntityQueryBuilder<TEntity>
        where TEntity : IEntity<ILocator>
    {
        private readonly EntityContext context;

        internal EntityQueryBuilder(EntityContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Query entities by performing a search.
        /// </summary>
        public Query Search<TQuery>(TQuery query)
        {
            return new Query(
                this.context,
                ct => this.context.SearchAsync<TQuery, TEntity>(query, ct));
        }

        /// <summary>
        /// Query entities by performing a wildcard search.
        /// </summary>
        public Query List()
        {
            return Search(WildcardQuery.Instance);
        }

        /// <summary>
        /// Query entities by parent locator.
        /// </summary>
        /// <param name="locator">Locator of parent entity</param>
        public Query ByAncestor(ILocator locator)
        {
            return new Query(
                this.context,
                ct => this.context.ListDescendantsAsync<TEntity>(locator, ct));
        }

        public class Query
        {
            private readonly EntityContext context;
            private readonly QueryDelegate<TEntity> query;
            private readonly Dictionary<Type, Func<ILocator, CancellationToken, Task<object?>>> aspects
                = new Dictionary<Type, Func<ILocator, CancellationToken, Task<object?>>>();
            private readonly Dictionary<Type, DeriveAspectDelegate> derivedAspects
                = new Dictionary<Type, DeriveAspectDelegate>();

            internal Query(
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
            public Query IncludeAspect<TAspect>()
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
            public Query IncludeAspect<TInputAspect, TAspect>(
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
            /// Execute query.
            /// </summary>
            /// <returns>
            /// Observable collection of entities and their aspects.
            /// </returns>
            public Task<ObservableEntityQueryResult<TEntity>> ExecuteObservableAsync(
                CancellationToken cancellationToken)
            {
                // 1. Subscribe to eventqueue
                //    pass subscription to observable collection for disposal.
                // 
                throw new NotImplementedException();
            }
        }
    }
}