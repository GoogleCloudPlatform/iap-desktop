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
    /// entity containers and aspect providers.
    /// </summary>
    public class EntityContext
    {
        private readonly IDictionary<Type, LocatorConfiguration> locators;

        public EntityContext(IServiceCategoryProvider serviceProvider) : this(new Builder()
            .AddContainers(serviceProvider.GetServicesByCategory<IEntityContainer>())
            .AddSearchers(serviceProvider.GetServicesByCategory<IEntitySearcher>())
            .AddAspectProviders(serviceProvider.GetServicesByCategory<IEntityAspectProvider>()))
        {
        }

        private EntityContext(Builder builder)
        {
            this.locators = builder.BuildConfiguration();
        }

        //--------------------------------------------------------------------
        // Publics.
        //--------------------------------------------------------------------

        public void InvalidateItem(ILocator locator)
        {
            foreach (var container in this.locators.Values
                .SelectMany(c => c.EntityContainers))
            {
                container.Invalidate(locator);
            }
        }

        /// <summary>
        /// Check of there is any entity container for this type of locator.
        /// </summary>
        public bool IsContainer(Type locatorType)
        {
            if (this.locators.TryGetValue(locatorType, out var configuration))
            {
                return configuration.EntityContainers.Any();
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Check of there is any entity container for this type of locator.
        /// </summary>
        public bool IsContainer(ILocator locator)
        {
            return IsContainer(locator.GetType());
        }

        /// <summary>
        /// Check of there is any entity container for this type of locator.
        /// </summary>
        public bool IsContainer<TLocator>() where TLocator : ILocator
        {
            return IsContainer(typeof(TLocator));
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
        /// Query all containers that support the kind of locator and
        /// requested entity type, or a subtype thereof.
        /// </summary>
        public async Task<ICollection<TEntity>> ListAsync<TEntity>(
            ILocator locator,
            CancellationToken cancellationToken)
            where TEntity : IEntity
        {
            if (this.locators.TryGetValue(locator.GetType(), out var configuration))
            {
                var listTasks = configuration.EntityContainers
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
        /// Search all containers that support the kind of locator and
        /// requested entity type, or a subtype thereof.
        /// </summary>
        public async Task<ICollection<TEntity>> SearchAsync<TEntity>(
            string query, 
            CancellationToken cancellationToken)
            where TEntity : IEntity
        {
            var searchTasks = this.locators.Values
                .SelectMany(c => c.EntitySearchers)
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
                .ToList();
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
            public ICollection<RegisteredEntityContainer> EntityContainers { get; }
            public ICollection<RegisteredEntitySearcher> EntitySearchers { get; }
            public ICollection<RegisteredAspectProvider> AspectProviders { get; }
            public ICollection<RegisteredAsyncAspectProvider> AsyncAspectProviders { get; }

            public LocatorConfiguration(
                Type locatorType,
                ICollection<RegisteredEntityContainer> entityContainers,
                ICollection<RegisteredEntitySearcher> entitySearchers,
                ICollection<RegisteredAspectProvider> aspectProviders,
                ICollection<RegisteredAsyncAspectProvider> asyncAspectProviders)
            {
                this.LocatorType = locatorType;
                this.EntityContainers = entityContainers;
                this.EntitySearchers = entitySearchers;
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

        internal readonly struct RegisteredEntityContainer
        {
            public delegate Task<ICollection<IEntity>> ListAsyncDelegate(
                ILocator locator,
                CancellationToken cancellationToken);

            public delegate void InvalidateDelegate(ILocator locator);

            public Type LocatorType { get; }
            public Type EntityType { get; }
            public ListAsyncDelegate ListAsync { get; }
            public InvalidateDelegate Invalidate { get; }

            public RegisteredEntityContainer(
                Type locatorType, 
                Type entityType, 
                ListAsyncDelegate listAsync,
                InvalidateDelegate invalidate)
            {
                this.LocatorType = locatorType;
                this.EntityType = entityType;
                this.ListAsync = listAsync;
                this.Invalidate = invalidate;
            }
        }

        internal readonly struct RegisteredEntitySearcher
        {
            public delegate Task<ICollection<IEntity>> SearchAsyncDelegate(
                string query,
                CancellationToken cancellationToken);

            public Type LocatorType { get; }
            public Type EntityType { get; }
            public SearchAsyncDelegate SearchAsync { get; }

            public RegisteredEntitySearcher(
                Type locatorType, 
                Type entityType,
                SearchAsyncDelegate searchAsync)
            {
                this.LocatorType = locatorType;
                this.EntityType = entityType;
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
            private readonly List<RegisteredEntityContainer> entityContainers = 
                new List<RegisteredEntityContainer>();
            private readonly List<RegisteredEntitySearcher> entitySearchers = 
                new List<RegisteredEntitySearcher>();
            private readonly List<RegisteredAspectProvider> aspectProviders = 
                new List<RegisteredAspectProvider>();
            private readonly List<RegisteredAsyncAspectProvider> asyncAspectProviders = 
                new List<RegisteredAsyncAspectProvider>();

            private readonly MethodInfo addContainerCoreMethod;
            private readonly MethodInfo addSearcherCoreMethod;
            private readonly MethodInfo addAspectProviderCoreMethod;
            private readonly MethodInfo addAsyncAspectProviderCoreMethod;

            public Builder()
            {
                this.addContainerCoreMethod = GetType().GetMethod(
                    nameof(AddContainerCore),
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

            private void AddContainerCore<TLocator, TEntity, TEntityLocator>(
                IEntityContainer<TLocator, TEntity, TEntityLocator> container)
                where TLocator : ILocator
                where TEntityLocator : ILocator
                where TEntity : IEntity<TEntityLocator>
            {
                this.entityContainers.Add(new RegisteredEntityContainer(
                    typeof(TLocator),
                    typeof(TEntity),
                    async (locator, cancellationToken) =>
                    {
                        Debug.Assert(locator is TLocator);
                        var result = await container
                            .ListAsync((TLocator)locator, cancellationToken)
                            .ConfigureAwait(false);

                        return result.Cast<IEntity>().ToList();
                    },
                    locator => container.Invalidate((TLocator)locator)));
            }

            private void AddSearcherCore<TLocator, TEntity>(
                IEntitySearcher<TLocator, TEntity> container)
                where TLocator : ILocator
                where TEntity : IEntity
            {
                this.entitySearchers.Add(new RegisteredEntitySearcher(
                    typeof(TLocator),
                    typeof(TEntity),
                    async (query, cancellationToken) =>
                    {
                        var result = await container
                            .SearchAsync(query, cancellationToken)
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

            public Builder AddContainer(IEntityContainer container)
            {
                foreach (var genericInterface in GetGenericInterfaces(
                    container.GetType(),
                    typeof(IEntityContainer<,,>)))
                {
                    this.addContainerCoreMethod
                        .MakeGenericMethod(genericInterface.GenericTypeArguments)
                        .Invoke(this, new object[] { container });
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

                return this;
            }

            public Builder AddContainers(IEnumerable<IEntityContainer> containers)
            {
                foreach (var container in containers)
                {
                    AddContainer(container);
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

            internal IDictionary<Type, LocatorConfiguration> BuildConfiguration()
            {
                return Enumerable.Empty<Type>()
                    .Concat(this.entityContainers.Select(c => c.LocatorType))
                    .Concat(this.entitySearchers.Select(c => c.LocatorType))
                    .Concat(this.aspectProviders.Select(c => c.LocatorType))
                    .Concat(this.asyncAspectProviders.Select(c => c.LocatorType))
                    .Distinct()
                    .ToDictionary(
                        type => type,
                        type => new LocatorConfiguration(
                            type,
                            this.entityContainers.Where(c => c.LocatorType == type).ToList(),
                            this.entitySearchers.Where(c => c.LocatorType == type).ToList(),
                            this.aspectProviders.Where(c => c.LocatorType == type).ToList(),
                            this.asyncAspectProviders.Where(c => c.LocatorType == type).ToList()));
            }

            public EntityContext Build()
            {
                return new EntityContext(this);
            }
        }
    }
}
