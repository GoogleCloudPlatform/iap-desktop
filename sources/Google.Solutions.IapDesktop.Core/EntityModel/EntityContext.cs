using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.EntityModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.EntityModel
{
    /// <summary>
    /// Provides a unified view over data exposed by multiple
    /// entity containers and aspect providers.
    /// </summary>
    public class EntityContext //: ISearchableEntityContainer<ILocator, IEntity>
    {
        private readonly List<Registration> registrations;

        public EntityContext(IServiceCategoryProvider serviceProvider)
            : this(new Builder()
                  .AddContainer(serviceProvider.GetServicesByCategory<IEntityContainer>())
                  .AddAspectProvider(serviceProvider.GetServicesByCategory<IEntityAspectProvider>()))
        {
        }

        private EntityContext(Builder builder)
        {
            throw new NotImplementedException();
        }

        //--------------------------------------------------------------------
        // Publics.
        //--------------------------------------------------------------------

        /// <summary>
        /// Check of there is any entity container for this type of locator.
        /// </summary>
        public bool IsContainer(ILocator locator)
        {
            return this.registrations
                .Where(r => r.LocatorType == locator.GetType())
                .SelectMany(r => r.EntityContainers)
                .Any();
        }

        public bool SupportsAspect<TAspect>(ILocator locator)
        {
            //
            // Check synchronous providers.
            //
            if (this.registrations
                .Where(r => r.LocatorType == locator.GetType())
                .SelectMany(r => r.AspectProviders)
                .Any(p => typeof(TAspect).IsAssignableFrom(p.AspectType)))
            {
                return true; 
            }

            //
            // Check asynchronous providers.
            //
            if (this.registrations
                .Where(r => r.LocatorType == locator.GetType())
                .SelectMany(r => r.AsyncAspectProviders)
                .Any(p => typeof(TAspect).IsAssignableFrom(p.AspectType)))
            {
                return true;
            }

            return false;
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
            var listTasks = this.registrations
                .Where(r => r.LocatorType == locator.GetType())
                .SelectMany(r => r.EntityContainers)
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

        public Task<ICollection<TEntity>> SearchAsync<TEntity>(
            string query, 
            CancellationToken cancellationToken)
            where TEntity : IEntity
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Query an aspect from synchronous and asynchronous providers.
        /// </summary>
        public Task<TAspect?> QueryAspectAsync<TAspect>(
            ILocator locator,
            CancellationToken cancellationToken)
            where TAspect : class
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Query an aspect from synchronous providers, ignoring 
        /// asynchronous providers.
        /// </summary>
        public TAspect? QueryAspect<TAspect>(
            ILocator locator,
            CancellationToken cancellationToken)
            where TAspect : class
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the default aspect of an enitity.
        /// </summary>
        public async Task<IEntity?> QueryAspectAsync(
            ILocator locator,
            CancellationToken cancellationToken)
        { 
            return await QueryAspectAsync<IEntity>(locator, cancellationToken)
                .ConfigureAwait(false);
        }

        //--------------------------------------------------------------------
        // Registration data structures.
        //--------------------------------------------------------------------

        private struct Registration
        {
            public Type LocatorType { get; }
            public Collection<RegisteredEntityContainer> EntityContainers { get; }
            public Collection<RegisteredEntitySearcher> EntitySearchers { get; }
            public Collection<RegisteredAspectProvider> AspectProviders { get; }
            public Collection<RegisteredAsyncAspectProvider> AsyncAspectProviders { get; }

            public Registration(
                Type locatorType,
                Collection<RegisteredEntityContainer> entityContainers,
                Collection<RegisteredEntitySearcher> entitySearchers,
                Collection<RegisteredAspectProvider> aspectProviders,
                Collection<RegisteredAsyncAspectProvider> asyncAspectProviders)
            {
                this.LocatorType = locatorType;
                this.EntityContainers = entityContainers;
                this.EntitySearchers = entitySearchers;
                this.AspectProviders = aspectProviders;
                this.AsyncAspectProviders = asyncAspectProviders;
            }
        }

        private struct RegisteredEntityContainer
        {
            public delegate Task<ICollection<IEntity>> ListAsyncDelegate(
                ILocator locator,
                CancellationToken cancellationToken);

            public Type EntityType { get; }
            public ListAsyncDelegate ListAsync { get; }
        }

        private struct RegisteredEntitySearcher
        {
            public delegate Task<ICollection<IEntity>> SearchAsyncDelegate(
                string query,
                CancellationToken cancellationToken);

            public Type EntityType { get; }
            public SearchAsyncDelegate SearchAsync { get; }
        }

        private struct RegisteredAspectProvider
        {
            public delegate object? QueryAspectDelegate(
                ILocator locator,
                CancellationToken cancellationToken);

            public Type AspectType { get; }
            public QueryAspectDelegate QueryAspect { get; }
        }

        private struct RegisteredAsyncAspectProvider
        {
            public delegate Task<object?> QueryAspectAsyncDelegate(
                ILocator locator,
                CancellationToken cancellationToken);

            public Type AspectType { get; }
            public QueryAspectAsyncDelegate QueryAspectAsync { get; }
        }

        //--------------------------------------------------------------------
        // Builder.
        //--------------------------------------------------------------------

        public class Builder
        {
            internal IDictionary<Type, ListDelegate> ListDelegates { get; }
                = new Dictionary<Type, ListDelegate>();

            internal IDictionary<Tuple<Type, Type>, GetAspectDelegate> GetAspectDelegates { get; }
                = new Dictionary<Tuple<Type, Type>, GetAspectDelegate>();

            private readonly MethodInfo registerEntityContainerMethod;
            private readonly MethodInfo registerEntityAspectProviderMethod;

            public Builder()
            {
                this.registerEntityContainerMethod = GetType().GetMethod(
                    nameof(AddTypedEntityContainer),
                    BindingFlags.Instance | BindingFlags.NonPublic);
                this.registerEntityAspectProviderMethod = GetType().GetMethod(
                    nameof(AddTypedEntityAspectProvider),
                    BindingFlags.Instance | BindingFlags.NonPublic);
            }


            private void AddTypedEntityContainer<TLocator, TEntity>(
                IEntityContainer<TLocator, TEntity> container)
                where TLocator : ILocator
                where TEntity : IEntity
            {
                Precondition.Expect(
                    !this.ListDelegates.ContainsKey(typeof(TLocator)),
                    "A different provider has been registered for this locator type");

                this.ListDelegates[typeof(TLocator)] =
                    async (locator, cancellationToken) =>
                    {
                        Debug.Assert(locator is TLocator);
                        var result = await container
                            .ListAsync((TLocator)locator, cancellationToken)
                            .ConfigureAwait(false);

                        return result.Cast<IEntity>().ToList();
                    };
            }

            private void AddTypedEntityAspectProvider<TLocator, TAspect>(
                IAsyncEntityAspectProvider<TLocator, TAspect> provider)
                where TLocator : ILocator
                where TAspect : class
            {
                var key = Tuple.Create(typeof(TLocator), typeof(TAspect));

                Precondition.Expect(
                    !this.GetAspectDelegates.ContainsKey(key),
                    "A different provider has been registered for this locator and aspect type");

                this.GetAspectDelegates[key] = async (locator, cancellationToken) =>
                {
                    Debug.Assert(locator is TLocator);
                    var result = await provider
                        .QueryAspectAsync((TLocator)locator, cancellationToken)
                        .ConfigureAwait(false);

                    Debug.Assert(result != null);
                    return result!;
                };
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

            public Builder AddContainer(IEntityContainer container)
            {
                foreach (var genericInterface in GetGenericInterfaces(
                    container.GetType(),
                    typeof(IEntityContainer<,>)))
                {
                    this.registerEntityContainerMethod
                        .MakeGenericMethod(genericInterface.GenericTypeArguments)
                        .Invoke(this, new object[] { container });
                }

                return this;
            }


            public Builder AddAspectProvider(IEntityAspectProvider provider)
            {
                foreach (var genericInterface in GetGenericInterfaces(
                    provider.GetType(),
                    typeof(IAsyncEntityAspectProvider<,>)))
                {
                    this.registerEntityAspectProviderMethod
                        .MakeGenericMethod(genericInterface.GenericTypeArguments)
                        .Invoke(this, new object[] { provider });
                }

                return this;
            }


            public Builder AddContainer(IEnumerable<IEntityContainer> containers)
            {
                foreach (var container in containers)
                {
                    AddContainer(container);
                }

                return this;
            }

            public Builder AddAspectProvider(IEnumerable<IEntityAspectProvider> providers)
            {
                foreach (var provider in providers)
                {
                    AddAspectProvider(provider);
                }

                return this;
            }

            public EntityContext Build()
            {
                return new EntityContext(this);
            }
        }

    }
}
