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
    public class Workspace
    {
        public delegate Task<ICollection<IEntity>> ListDelegate(
            ILocator locator,
            CancellationToken cancellationToken);

        public delegate Task<object> GetAspectDelegate(
            ILocator locator,
            CancellationToken cancellationToken);

        private readonly Dictionary<Type, ListDelegate> listDelegates;
        private readonly Dictionary<Tuple<Type, Type>, GetAspectDelegate> getAspectDelegates;

        public Workspace(IServiceCategoryProvider serviceProvider)
            : this(
                  serviceProvider.GetServicesByCategory<IEntityContainer>(),
                  serviceProvider.GetServicesByCategory<IEntityAspectProvider>())
        {
        }

        /// <summary>
        /// Register all containers and aspect providers based on the
        /// generic interface they implement.
        /// </summary>
        internal Workspace(
            IEnumerable<IEntityContainer> entityContainers,
            IEnumerable<IEntityAspectProvider> aspectProviders) 
        {
            var registerEntityContainerMethod = GetType().GetMethod(
                nameof(RegisterEntityContainer),
                BindingFlags.Instance | BindingFlags.NonPublic);
            var registerEntityAspectProviderMethod = GetType().GetMethod(
                nameof(RegisterEntityAspectProvider),
                BindingFlags.Instance | BindingFlags.NonPublic);

            //
            // Register containers.
            //
            this.listDelegates = new Dictionary<Type, ListDelegate>();
            foreach (var container in entityContainers)
            {
                foreach (var genericInterface in GetGenericInterfaces(
                    container.GetType(),
                    typeof(IEntityContainer<,>)))
                {
                    registerEntityContainerMethod
                        .MakeGenericMethod(genericInterface.GenericTypeArguments)
                        .Invoke(this, new object[] { container });
                }
            }

            //
            // Register aspect providers. Each container is also 
            // an aspect provider.
            //
            this.getAspectDelegates = new Dictionary<Tuple<Type, Type>, GetAspectDelegate>();
            foreach (var provider in aspectProviders
                .Concat(entityContainers.Cast<IEntityAspectProvider>()))
            {
                foreach (var genericInterface in GetGenericInterfaces(
                    provider.GetType(),
                    typeof(IEntityAspectProvider<,>)))
                {
                    registerEntityAspectProviderMethod
                        .MakeGenericMethod(genericInterface.GenericTypeArguments)
                        .Invoke(this, new object[] { provider });
                }
            }
        }

        //--------------------------------------------------------------------
        // IWorkspace.
        //--------------------------------------------------------------------

        public Task<ICollection<IEntity>> ListAsync(
            ILocator locator,
            CancellationToken cancellationToken)
        {
            //
            // Lookup the container that's responsible for this type of locator,
            // and use it to list descendents.
            //
            if (this.listDelegates.TryGetValue(
                locator.GetType(), 
                out var listFunc))
            {
                return listFunc(locator, cancellationToken);
            }
            else
            {
                return Task.FromResult<ICollection<IEntity>>(Array.Empty<IEntity>());
            }
        }

        public async Task<TAspect> GetAspectAsync<TAspect>(
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

        //--------------------------------------------------------------------
        // Helper methods, only used during object construction.
        //--------------------------------------------------------------------

        private void RegisterEntityContainer<TLocator, TEntity>(
            IEntityContainer<TLocator, TEntity> container)
            where TLocator : ILocator
            where TEntity : IEntity<TLocator>
        {
            Precondition.Expect(
                !this.listDelegates.ContainsKey(typeof(TLocator)),
                "A different provider has been registered for this locator type");

            this.listDelegates[typeof(TLocator)] = 
                async (locator, cancellationToken) =>
                {
                    Debug.Assert(locator is TLocator);
                    var result = await container
                        .ListAsync((TLocator)locator, cancellationToken)
                        .ConfigureAwait(false);

                    return result.Cast<IEntity>().ToList();
                };
        }

        private void RegisterEntityAspectProvider<TLocator, TAspect>(
            IEntityAspectProvider<TLocator, TAspect> provider)
            where TLocator : ILocator
        {
            var key = Tuple.Create(typeof(TLocator), typeof(TAspect));

            Precondition.Expect(
                !this.getAspectDelegates.ContainsKey(key),
                "A different provider has been registered for this locator and aspect type");

            this.getAspectDelegates[key] = async (locator, cancellationToken) =>
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
    }
}
