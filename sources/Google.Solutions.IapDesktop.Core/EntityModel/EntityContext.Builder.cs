﻿//
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

namespace Google.Solutions.IapDesktop.Core.EntityModel
{
    public partial class EntityContext
    {
        /// <summary>
        /// Builder for EntityContext objects.
        /// </summary>
        public class Builder
        {
            private readonly List<RegisteredEntityCache> entityCaches =
                new List<RegisteredEntityCache>();
            private readonly List<RegisteredEntityNavigator> entityNavigators =
                new List<RegisteredEntityNavigator>();
            private readonly List<RegisteredEntitySearcher> entitySearchers =
                new List<RegisteredEntitySearcher>();
            private readonly List<RegisteredAspectProvider> aspectProviders =
                new List<RegisteredAspectProvider>();
            private readonly List<RegisteredAsyncAspectProvider> asyncAspectProviders =
                new List<RegisteredAsyncAspectProvider>();

            private readonly MethodInfo addCacheCoreMethod;
            private readonly MethodInfo addNavigatorCoreMethod;
            private readonly MethodInfo addSearcherCoreMethod;
            private readonly MethodInfo addAspectProviderCoreMethod;
            private readonly MethodInfo addAsyncAspectProviderCoreMethod;

            public IEventQueue EventQueue { get; }

            public Builder(IEventQueue eventQueue)
            {
                this.EventQueue = eventQueue;

                this.addCacheCoreMethod = GetType().GetMethod(
                    nameof(AddCacheCore),
                    BindingFlags.Instance | BindingFlags.NonPublic);
                this.addNavigatorCoreMethod = GetType().GetMethod(
                    nameof(AddNavigatorCore),
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

                //
                // Enable introspection of registered searchers.
                //
                var introspector = new Introspector(this.entitySearchers);
                AddSearcher(introspector);
                AddNavigator(introspector);
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

            private void AddNavigatorCore<TLocator, TEntity>(
                IEntityNavigator<TLocator, TEntity> navigator)
                where TLocator : ILocator
                where TEntity : IEntity<ILocator>
            {
                this.entityNavigators.Add(new RegisteredEntityNavigator(
                    typeof(TLocator),
                    typeof(TEntity),
                    async (locator, cancellationToken) =>
                    {
                        Debug.Assert(locator is TLocator);
                        var result = await navigator
                            .ListDescendantsAsync((TLocator)locator, cancellationToken)
                            .ConfigureAwait(false);

                        return result.Cast<IEntity<ILocator>>().ToList();
                    }));
            }

            private void AddSearcherCore<TQuery, TEntity>(
                IEntitySearcher<TQuery, TEntity> searcher)
                where TEntity : IEntity<ILocator>
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

                        return result.Cast<IEntity<ILocator>>().ToList();
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

            public Builder AddNavigator(IEntityNavigator navigator)
            {
                foreach (var genericInterface in GetGenericInterfaces(
                    navigator.GetType(),
                    typeof(IEntityNavigator<,>)))
                {
                    this.addNavigatorCoreMethod
                        .MakeGenericMethod(genericInterface.GenericTypeArguments)
                        .Invoke(this, new object[] { navigator });
                }

                if (navigator is IEntityCache cache)
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

            public Builder AddNavigators(IEnumerable<IEntityNavigator> navigator)
            {
                foreach (var container in navigator)
                {
                    AddNavigator(container);
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
                    .Concat(this.entityNavigators.Select(c => c.LocatorType))
                    .Concat(this.aspectProviders.Select(c => c.LocatorType))
                    .Concat(this.asyncAspectProviders.Select(c => c.LocatorType))
                    .Distinct()
                    .ToDictionary(
                        type => type,
                        type => new LocatorConfiguration(
                            type,
                            this.entityNavigators.Where(c => c.LocatorType == type).ToList(),
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
