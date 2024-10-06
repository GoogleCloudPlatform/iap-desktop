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
using Google.Solutions.IapDesktop.Core.EntityModel.Query;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.EntityModel
{
    /// <summary>
    /// Provides a unified view over data exposed by multiple
    /// entity expanders and aspect providers.
    /// </summary>
    public partial class EntityContext
    {
        private readonly IList<RegisteredEntityCache> caches;
        private readonly IDictionary<Type, List<RegisteredEntitySearcher>> searchers;
        private readonly IDictionary<Type, LocatorConfiguration> locators;

        public EntityContext(IServiceCategoryProvider serviceProvider) : this(new Builder()
            .AddExpanders(serviceProvider.GetServicesByCategory<IEntityExpander>())
            .AddSearchers(serviceProvider.GetServicesByCategory<IEntitySearcher>())
            .AddAspectProviders(serviceProvider.GetServicesByCategory<IEntityAspectProvider>()))
        {
        }

        private EntityContext(Builder builder)
        {
            this.locators = builder.BuildLocatorConfiguration();
            this.caches = builder.BuildCaches();
            this.searchers = builder.BuildSearchers();
        }

        //--------------------------------------------------------------------
        // Publics.
        //--------------------------------------------------------------------

        /// <summary>
        /// Check of there is any entity expander for this type of locator.
        /// </summary>
        public bool SupportsExpansion(Type locatorType)
        {
            if (this.locators.TryGetValue(locatorType, out var configuration))
            {
                return configuration.Expanders.Any();
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Check of there is any entity expander for this type of locator.
        /// </summary>
        public bool SupportsExpansion(ILocator locator)
        {
            return SupportsExpansion(locator.GetType());
        }

        /// <summary>
        /// Check of there is any entity expander for this type of locator.
        /// </summary>
        public bool SupportsExpansion<TLocator>() where TLocator : ILocator
        {
            return SupportsExpansion(typeof(TLocator));
        }

        /// <summary>
        /// Check if a given aspect if supported for a type of locator.
        /// </summary>
        public bool SupportsAspect(Type locatorType, Type aspectType)
        {
            if (this.locators.TryGetValue(locatorType, out var configuration))
            {
                //
                // Check synchronous providers.
                //
                if (configuration.AspectProviders
                    .Any(p => aspectType.IsAssignableFrom(p.AspectType)))
                {
                    return true;
                }

                //
                // Check asynchronous providers.
                //
                if (configuration.AsyncAspectProviders
                    .Any(p => aspectType.IsAssignableFrom(p.AspectType)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if a given aspect if supported for a type of locator.
        /// </summary>
        public bool SupportsAspect<TLocator, TAspect>() where TLocator : ILocator
        {
            return SupportsAspect(typeof(TLocator), typeof(TAspect));
        }

        /// <summary>
        /// Invalidate cached entities for a locator.
        /// </summary>
        public void Invalidate(ILocator locator)
        {
            foreach (var cache in this.caches.Where(c => c.LocatorType == locator.GetType()))
            {
                cache.Invalidate(locator);
            }
        }

        /// <summary>
        /// Query all expanders that support the kind of locator and
        /// requested entity type, or a subtype thereof.
        /// </summary>
        public async Task<ICollection<TEntity>> ExpandAsync<TEntity>(
            ILocator locator,
            CancellationToken cancellationToken)
            where TEntity : IEntity<ILocator>
        {
            if (this.locators.TryGetValue(locator.GetType(), out var configuration))
            {
                var listTasks = configuration.Expanders
                    .Where(c => typeof(TEntity).IsAssignableFrom(c.EntityType))
                    .Select(c => c.ExpandAsync(locator, cancellationToken))
                    .ToList();

                var listResults = await Task
                    .WhenAll(listTasks)
                    .ConfigureAwait(false);

                //
                // Flatten result and cast to requested type.
                //
                return listResults
                    .SelectMany(r => r)
                    .Cast<TEntity>()
                    .ToList();
            }
            else
            {
                return Array.Empty<TEntity>();
            }
        }

        /// <summary>
        /// Search for entities of the requested entity type that match a query.
        /// </summary>
        public async Task<ICollection<TEntity>> SearchAsync<TQuery, TEntity>(
            TQuery query, 
            CancellationToken cancellationToken)
            where TEntity : IEntity<ILocator>
        {
            if (query != null &&
                this.searchers.TryGetValue(typeof(TQuery), out var searchers))
            {
                var searchTasks = searchers
                    .Where(s => typeof(TEntity).IsAssignableFrom(s.EntityType))
                    .Select(s => s.SearchAsync(query, cancellationToken))
                    .ToList();

                var searchResults = await Task
                    .WhenAll(searchTasks)
                    .ConfigureAwait(false);

                //
                // Flatten result and cast to requested type.
                //

                return searchResults
                    .SelectMany(r => r)
                    .Cast<TEntity>()
                    .OrderBy(e => e.GetType().Name).ThenBy(e => e.DisplayName)
                    .ToList();
            }
            else
            {
                return Array.Empty<TEntity>();
            }
        }

        /// <summary>
        /// Query an aspect from synchronous providers, ignoring 
        /// asynchronous providers.
        /// </summary>
        public TAspect? QueryAspect<TAspect>(ILocator locator)
            where TAspect : class
        {
            if (this.locators.TryGetValue(locator.GetType(), out var configuration))
            {
                return configuration.AspectProviders
                    .Where(c => typeof(TAspect).IsAssignableFrom(c.AspectType))
                    .Select(c => c.QueryAspect(locator))
                    .FirstOrDefault() as TAspect;
            }

            return null;
        }

        /// <summary>
        /// Query an aspect from synchronous and asynchronous providers.
        /// </summary>
        public async Task<TAspect?> QueryAspectAsync<TAspect>(
            ILocator locator,
            CancellationToken cancellationToken)
            where TAspect : class
        {
            //
            // Try synchronous providers.
            //
            if (QueryAspect<TAspect>(locator) is TAspect aspect)
            {
                return aspect;
            }

            //
            // Try asynchronous providers.
            //
            if (this.locators.TryGetValue(locator.GetType(), out var configuration))
            {
                var queryTask = configuration.AsyncAspectProviders
                    .Where(c => typeof(TAspect).IsAssignableFrom(c.AspectType))
                    .Select(p => p.QueryAspectAsync(locator, cancellationToken))
                    .FirstOrDefault();

                if (queryTask != null)
                {
                    return (await queryTask.ConfigureAwait(false)) as TAspect;
                }
            }

            return null;
        }

        /// <summary>
        /// Create a query builder for this context.
        /// </summary>
        public EntityQueryBuilder<TEntity> Entities<TEntity>()
            where TEntity : IEntity<ILocator>
        {
            return new EntityQueryBuilder<TEntity>(this);
        }

        //--------------------------------------------------------------------
        // Introspection
        //--------------------------------------------------------------------

        /// <summary>
        /// Enables introspecting the entity types supported by this context.
        /// </summary>
        private class Introspector
            : IEntitySearcher<AnyQuery, EntityType>, IEntityExpander<EntityTypeLocator, IEntity<ILocator>>
        {
            private readonly ICollection<RegisteredEntitySearcher> searchers;

            public Introspector(ICollection<RegisteredEntitySearcher> searchers)
            {
                this.searchers = searchers;
            }

            /// <summary>
            /// List all searchable entity types. Individual entity types
            /// can then be expanded to list actual entities.
            /// </summary>
            public Task<IEnumerable<EntityType>> SearchAsync( // TODO: test
                AnyQuery query,
                CancellationToken cancellationToken)
            {
                return Task.FromResult<IEnumerable<EntityType>>(this.searchers
                    .Select(s => new EntityType(s.EntityType))
                    .ToList());
            }

            /// <summary>
            /// List entities of a certain type.
            /// </summary>
            public Task<IEnumerable<IEntity<ILocator>>> ExpandAsync(
                EntityTypeLocator typeLocator,
                CancellationToken cancellationToken)
            {
                //
                // Look for a suitable searcher.
                //
                var searcher = this.searchers.Where(s =>
                    s.EntityType == typeLocator.Type && s.QueryType == typeof(AnyQuery));
                if (searcher.Any())
                {
                    return searcher
                        .First()
                        .SearchAsync(AnyQuery.Instance, cancellationToken);
                }
                else
                {
                    return Task.FromResult(Enumerable.Empty<IEntity<ILocator>>());
                }
            }
        }

        //--------------------------------------------------------------------
        // Registration data structures.
        //--------------------------------------------------------------------

        /// <summary>
        /// Configuration for a type of locator.
        /// </summary>
        internal readonly struct LocatorConfiguration
        {
            public Type LocatorType { get; }
            public ICollection<RegisteredEntityExpander> Expanders { get; }
            public ICollection<RegisteredAspectProvider> AspectProviders { get; }
            public ICollection<RegisteredAsyncAspectProvider> AsyncAspectProviders { get; }

            public LocatorConfiguration(
                Type locatorType,
                ICollection<RegisteredEntityExpander> expanders,
                ICollection<RegisteredAspectProvider> aspectProviders,
                ICollection<RegisteredAsyncAspectProvider> asyncAspectProviders)
            {
                this.LocatorType = locatorType;
                this.Expanders = expanders;
                this.AspectProviders = aspectProviders;
                this.AsyncAspectProviders = asyncAspectProviders;

                if (Enumerable.Empty<Type>()
                    .Concat(aspectProviders.Select(p => p.AspectType))
                    .Concat(asyncAspectProviders.Select(p => p.AspectType))
                    .GroupBy(t => t)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .FirstOrDefault() is Type duplicateAspect)
                {
                    throw new InvalidOperationException(
                        "Only one aspect provider can be registerd for " +
                        $"aspect {duplicateAspect}");
                }
            }
        }

        internal readonly struct RegisteredEntityCache
        {
            public delegate void InvalidateDelegate(ILocator locator);

            public Type LocatorType { get; }
            public InvalidateDelegate Invalidate { get; }

            public RegisteredEntityCache(Type locatorType, InvalidateDelegate invalidate)
            {
                this.LocatorType = locatorType;
                this.Invalidate = invalidate;
            }
        }

        internal readonly struct RegisteredEntityExpander
        {
            public delegate Task<IEnumerable<IEntity<ILocator>>> ExpandAsyncDelegate(
                ILocator locator,
                CancellationToken cancellationToken);

            public Type LocatorType { get; }
            public Type EntityType { get; }
            public ExpandAsyncDelegate ExpandAsync { get; }

            public RegisteredEntityExpander(
                Type locatorType, 
                Type entityType, 
                ExpandAsyncDelegate listAsync)
            {
                this.LocatorType = locatorType;
                this.EntityType = entityType;
                this.ExpandAsync = listAsync;
            }
        }

        internal readonly struct RegisteredEntitySearcher
        {
            public delegate Task<IEnumerable<IEntity<ILocator>>> SearchAsyncDelegate(
                object query,
                CancellationToken cancellationToken);

            public Type EntityType { get; }
            public Type QueryType { get; }
            public SearchAsyncDelegate SearchAsync { get; }

            public RegisteredEntitySearcher(
                Type entityType,
                Type queryType,
                SearchAsyncDelegate searchAsync)
            {
                this.EntityType = entityType;
                this.QueryType = queryType;
                this.SearchAsync = searchAsync;
            }
        }

        internal readonly struct RegisteredAspectProvider
        {
            public delegate object? QueryAspectDelegate(ILocator locator);

            public Type LocatorType { get; }
            public Type AspectType { get; }
            public QueryAspectDelegate QueryAspect { get; }

            public RegisteredAspectProvider(
                Type locatorType,
                Type aspectType,
                QueryAspectDelegate queryAspect)
            {
                this.LocatorType = locatorType;
                this.AspectType = aspectType;
                this.QueryAspect = queryAspect;
            }
        }

        internal readonly struct RegisteredAsyncAspectProvider
        {
            public delegate Task<object?> QueryAspectAsyncDelegate(
                ILocator locator,
                CancellationToken cancellationToken);

            public Type LocatorType { get; }
            public Type AspectType { get; }
            public QueryAspectAsyncDelegate QueryAspectAsync { get; }

            public RegisteredAsyncAspectProvider(
                Type locatorType, 
                Type aspectType, 
                QueryAspectAsyncDelegate queryAspectAsync)
            {
                this.LocatorType = locatorType;
                this.AspectType = aspectType;
                this.QueryAspectAsync = queryAspectAsync;
            }
        }
    }
}
