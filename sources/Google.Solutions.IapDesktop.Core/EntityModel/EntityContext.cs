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
using Google.Solutions.IapDesktop.Core.ObjectModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Google.Solutions.Common.Linq;

namespace Google.Solutions.IapDesktop.Core.EntityModel
{
    /// <summary>
    /// Provides a unified view over data exposed by multiple
    /// entity expanders and aspect providers.
    /// </summary>
    public class EntityContext
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
            foreach (var cache in this.caches)
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
            where TEntity : IEntity
        {
            if (this.locators.TryGetValue(locator.GetType(), out var configuration))
            {
                var listTasks = configuration.Expanders
                    .Where(c => typeof(TEntity).IsAssignableFrom(c.EntityType))
                    .Select(c => c.ListAsync(locator, cancellationToken))
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
            where TEntity : IEntity
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

                // TODO: Order by type, displayname
                return searchResults
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
            public delegate Task<ICollection<IEntity>> ListAsyncDelegate(
                ILocator locator,
                CancellationToken cancellationToken);

            public Type LocatorType { get; }
            public Type EntityType { get; }
            public ListAsyncDelegate ListAsync { get; }

            public RegisteredEntityExpander(
                Type locatorType, 
                Type entityType, 
                ListAsyncDelegate listAsync)
            {
                this.LocatorType = locatorType;
                this.EntityType = entityType;
                this.ListAsync = listAsync;
            }
        }

        internal readonly struct RegisteredEntitySearcher
        {
            public delegate Task<ICollection<IEntity>> SearchAsyncDelegate(
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

        //--------------------------------------------------------------------
        // Builder.
        //--------------------------------------------------------------------

        public class Builder
        {
            private readonly List<RegisteredEntityCache> entityCaches =
                new List<RegisteredEntityCache>();
            private readonly List<RegisteredEntityExpander> entityExpanders = 
                new List<RegisteredEntityExpander>();
            private readonly List<RegisteredEntitySearcher> entitySearchers = 
                new List<RegisteredEntitySearcher>();
            private readonly List<RegisteredAspectProvider> aspectProviders = 
                new List<RegisteredAspectProvider>();
            private readonly List<RegisteredAsyncAspectProvider> asyncAspectProviders = 
                new List<RegisteredAsyncAspectProvider>();

            private readonly MethodInfo addCacheCoreMethod;
            private readonly MethodInfo addExpanderCoreMethod;
            private readonly MethodInfo addSearcherCoreMethod;
            private readonly MethodInfo addAspectProviderCoreMethod;
            private readonly MethodInfo addAsyncAspectProviderCoreMethod;

            public Builder()
            {
                this.addCacheCoreMethod = GetType().GetMethod(
                    nameof(AddCacheCore),
                    BindingFlags.Instance | BindingFlags.NonPublic);
                this.addExpanderCoreMethod = GetType().GetMethod(
                    nameof(AddExpanderCore),
                    BindingFlags.Instance | BindingFlags.NonPublic);
                this.addSearcherCoreMethod = GetType().GetMethod(
                    nameof(AddSearcherCore),
                    BindingFlags.Instance | BindingFlags.NonPublic);
                this.addAspectProviderCoreMethod = GetType().GetMethod(
                    nameof(AddAspectProviderCore),
                    BindingFlags.Instance | BindingFlags.NonPublic);
                this.addAsyncAspectProviderCoreMethod = GetType().GetMethod(
                    nameof(AddAsyncAspectProviderCore),
                    BindingFlags.Instance | BindingFlags.NonPublic);
            }

            /// <summary>
            /// Lookup a type's implemented interface based on its unbound type.
            /// </summary>
            private static IEnumerable<Type> GetGenericInterfaces(
                Type type,
                Type unboundType)
            {
                return type
                    .GetInterfaces()
                    .Where(
                        i => i.IsGenericType &&
                        i.GetGenericTypeDefinition() == unboundType);
            }

            private void AddCacheCore<TLocator>(IEntityCache<TLocator> cache)
                where TLocator : ILocator
            {
                this.entityCaches.Add(new RegisteredEntityCache(
                    typeof(TLocator),
                    locator => cache.Invalidate((TLocator)locator)));
            }

            private void AddExpanderCore<TLocator, TEntity, TEntityLocator>(
                IEntityExpander<TLocator, TEntity, TEntityLocator> expander)
                where TLocator : ILocator
                where TEntityLocator : ILocator
                where TEntity : IEntity<TEntityLocator>
            {
                this.entityExpanders.Add(new RegisteredEntityExpander(
                    typeof(TLocator),
                    typeof(TEntity),
                    async (locator, cancellationToken) =>
                    {
                        Debug.Assert(locator is TLocator);
                        var result = await expander
                            .ExpandAsync((TLocator)locator, cancellationToken)
                            .ConfigureAwait(false);

                        return result.Cast<IEntity>().ToList();
                    }));
            }

            private void AddSearcherCore<TQuery, TEntity>(
                IEntitySearcher<TQuery, TEntity> searcher)
                where TEntity : IEntity
            {
                this.entitySearchers.Add(new RegisteredEntitySearcher(
                    typeof(TEntity),
                    typeof(TQuery),
                    async (query, cancellationToken) =>
                    {
                        Debug.Assert(query is TQuery);
                        var result = await searcher
                            .SearchAsync((TQuery)query, cancellationToken)
                            .ConfigureAwait(false);

                        return result.Cast<IEntity>().ToList();
                    }));
            }

            private void AddAspectProviderCore<TLocator, TAspect>(
                IEntityAspectProvider<TLocator, TAspect> provider)
                where TLocator : ILocator
                where TAspect : class
            {
                this.aspectProviders.Add(new RegisteredAspectProvider(
                    typeof(TLocator),
                    typeof(TAspect),
                    locator =>
                    {
                        Debug.Assert(locator is TLocator);
                        return provider.QueryAspect((TLocator)locator);
                    }));
            }

            private void AddAsyncAspectProviderCore<TLocator, TAspect>(
                IAsyncEntityAspectProvider<TLocator, TAspect> provider)
                where TLocator : ILocator
                where TAspect : class
            {
                this.asyncAspectProviders.Add(new RegisteredAsyncAspectProvider(
                    typeof(TLocator),
                    typeof(TAspect),
                    async (locator, cancellationToken) =>
                    {
                        Debug.Assert(locator is TLocator);
                        var result = await provider
                            .QueryAspectAsync((TLocator)locator, cancellationToken)
                            .ConfigureAwait(false);

                        Debug.Assert(result != null);
                        return result!;
                    }));
            }

            internal Builder AddCache(IEntityCache cache)
            {
                foreach (var genericInterface in GetGenericInterfaces(
                    cache.GetType(),
                    typeof(IEntityCache<>)))
                {
                    this.addCacheCoreMethod
                        .MakeGenericMethod(genericInterface.GenericTypeArguments)
                        .Invoke(this, new object[] { cache });
                }

                return this;
            }

            public Builder AddExpander(IEntityExpander expander)
            {
                foreach (var genericInterface in GetGenericInterfaces(
                    expander.GetType(),
                    typeof(IEntityExpander<,,>)))
                {
                    this.addExpanderCoreMethod
                        .MakeGenericMethod(genericInterface.GenericTypeArguments)
                        .Invoke(this, new object[] { expander });
                }

                if (expander is IEntityCache cache)
                {
                    AddCache(cache);
                }

                return this;
            }

            public Builder AddSearcher(IEntitySearcher searcher)
            {
                foreach (var genericInterface in GetGenericInterfaces(
                    searcher.GetType(),
                    typeof(IEntitySearcher<,>)))
                {
                    this.addSearcherCoreMethod
                        .MakeGenericMethod(genericInterface.GenericTypeArguments)
                        .Invoke(this, new object[] { searcher });
                }

                if (searcher is IEntityCache cache)
                {
                    AddCache(cache);
                }

                return this;
            }

            public Builder AddAspectProvider(IEntityAspectProvider provider)
            {
                //
                // Synchrounous.
                //
                foreach (var genericInterface in GetGenericInterfaces(
                    provider.GetType(),
                    typeof(IEntityAspectProvider<,>)))
                {
                    this.addAspectProviderCoreMethod
                        .MakeGenericMethod(genericInterface.GenericTypeArguments)
                        .Invoke(this, new object[] { provider });
                }

                //
                // Asynchrounous.
                //
                foreach (var genericInterface in GetGenericInterfaces(
                    provider.GetType(),
                    typeof(IAsyncEntityAspectProvider<,>)))
                {
                    this.addAsyncAspectProviderCoreMethod
                        .MakeGenericMethod(genericInterface.GenericTypeArguments)
                        .Invoke(this, new object[] { provider });
                }

                if (provider is IEntityCache cache)
                {
                    AddCache(cache);
                }

                return this;
            }

            public Builder AddExpanders(IEnumerable<IEntityExpander> expander)
            {
                foreach (var container in expander)
                {
                    AddExpander(container);
                }

                return this;
            }

            public Builder AddSearchers(IEnumerable<IEntitySearcher> searchers)
            {
                foreach (var searcher in searchers)
                {
                    AddSearcher(searcher);
                }

                return this;
            }

            public Builder AddAspectProviders(IEnumerable<IEntityAspectProvider> providers)
            {
                foreach (var provider in providers)
                {
                    AddAspectProvider(provider);
                }

                return this;
            }

            internal IDictionary<Type, LocatorConfiguration> BuildLocatorConfiguration()
            {
                return Enumerable.Empty<Type>()
                    .Concat(this.entityExpanders.Select(c => c.LocatorType))
                    .Concat(this.aspectProviders.Select(c => c.LocatorType))
                    .Concat(this.asyncAspectProviders.Select(c => c.LocatorType))
                    .Distinct()
                    .ToDictionary(
                        type => type,
                        type => new LocatorConfiguration(
                            type,
                            this.entityExpanders.Where(c => c.LocatorType == type).ToList(),
                            this.aspectProviders.Where(c => c.LocatorType == type).ToList(),
                            this.asyncAspectProviders.Where(c => c.LocatorType == type).ToList()));
            }

            internal List<RegisteredEntityCache> BuildCaches()
            {
                return this.entityCaches;
            }

            internal IDictionary<Type, List<RegisteredEntitySearcher>> BuildSearchers()
            {
                return this.entitySearchers
                    .GroupBy(s => s.QueryType)
                    .ToDictionary(g => g.Key, g => g.ToList());
            }

            public EntityContext Build()
            {
                return new EntityContext(this);
            }
        }
    }
}
