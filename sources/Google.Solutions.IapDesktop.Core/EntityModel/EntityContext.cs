using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ResourceModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
        public delegate Task<ICollection<IEntity>> ListDelegate(
            ILocator locator,
            CancellationToken cancellationToken);

        public delegate Task<object> GetAspectDelegate(
            ILocator locator,
            CancellationToken cancellationToken);

        private readonly IDictionary<Type, ListDelegate> listDelegates;
        private readonly IDictionary<Tuple<Type, Type>, GetAspectDelegate> getAspectDelegates;

        public EntityContext(IServiceCategoryProvider serviceProvider)
            : this(new Builder()
                  .AddContainer(serviceProvider.GetServicesByCategory<IEntityContainer>())
                  .AddAspectProvider(serviceProvider.GetServicesByCategory<IEntityAspectProvider>()))
        {
        }

        private EntityContext(Builder builder)
        {
            this.listDelegates = builder.ListDelegates;
            this.getAspectDelegates = builder.GetAspectDelegates;
        }

        //--------------------------------------------------------------------
        // Publics.
        //--------------------------------------------------------------------

        public bool IsContainer(ILocator locator)
        {
            return this.listDelegates.ContainsKey(locator.GetType());
        }

        public bool HasAspect<TAspect>(ILocator locator)
        {
            return this.getAspectDelegates.ContainsKey(Tuple.Create(locator.GetType(), typeof(TAspect)));
        }

        public Task<ICollection<TEntity>> ListAsync<TEntity>(
            ILocator locator,
            CancellationToken cancellationToken)
            where TEntity : IEntity
        {
            //
            // Query all containers that support the kind of locator and
            // requested entity.
            //
            throw new NotImplementedException();
        }

        public Task<ICollection<TEntity>> SearchAsync<TEntity>(
            string query, 
            CancellationToken cancellationToken)
            where TEntity : IEntity
        {
            throw new NotImplementedException();
        }

        public async Task<TAspect> GetAsync<TAspect>(
            ILocator locator,
            CancellationToken cancellationToken)
        {
            //
            // Lookup the provider that's responsible for this type of locator
            // and aspect type, and use it to get the requested data.
            //
            if (this.getAspectDelegates.TryGetValue(
                Tuple.Create(locator.GetType(), typeof(TAspect)), 
                out var getFunc))
            {
                var result = await getFunc(locator, cancellationToken).ConfigureAwait(false);
                return (TAspect)result;
            }
            else
            {
                throw new EntityAspectNotFoundException(locator, typeof(TAspect));
            }
        }

        /// <summary>
        /// Get the default aspect of an enitity.
        /// </summary>
        public async Task<IEntity> GetAsync(
            ILocator locator,
            CancellationToken cancellationToken)
        { 
            return await GetAsync<IEntity>(locator, cancellationToken)
                .ConfigureAwait(false);
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
                IEntityAspectProvider<TLocator, TAspect> provider)
                where TLocator : ILocator
            {
                var key = Tuple.Create(typeof(TLocator), typeof(TAspect));

                Precondition.Expect(
                    !this.GetAspectDelegates.ContainsKey(key),
                    "A different provider has been registered for this locator and aspect type");

                this.GetAspectDelegates[key] = async (locator, cancellationToken) =>
                {
                    Debug.Assert(locator is TLocator);
                    var result = await provider
                        .GetAsync((TLocator)locator, cancellationToken)
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
                    typeof(IEntityAspectProvider<,>)))
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
