using Google.Solutions.Apis.Locator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.ResourceModel
{
    public interface IEntityContainer
    {
        //Type LocatorType { get; }
        //IEntityContainer<ILocator, IEntity> Cast();
    }

    public interface IEntityContainer<TLocator, TEntity> :
        IEntityContainer, IEntityAspectProvider<TLocator, TEntity>
        where TLocator : ILocator
        where TEntity : IEntity<TLocator>
    {
        Task<ICollection<TEntity>> ListAsync(
            TLocator locator,
            CancellationToken cancellationToken);
    }

    public interface ISearchableEntityContainer<TLocator, TEntity> 
        : IEntityContainer<TLocator, TEntity>
        where TLocator : ILocator
        where TEntity : IEntity<TLocator>
    {
        Task<ICollection<TEntity>> SearchAsync(
            string query,
            CancellationToken cancellationToken);
    }

    public interface IEntityAspectProvider
    {
        //Type LocatorType { get; }
        //Type AspectType { get; }
    }

    public interface IEntityAspectProvider<TLocator, TAspect> : IEntityAspectProvider
        where TLocator : ILocator
    {
        Task<TAspect> GetAsync(
            TLocator locator,
            CancellationToken cancellationToken);
    }





    //public interface IEntityContainer
    //{
    //    /// <summary>
    //    /// Types of locators that this provider supports.
    //    /// Types must be derived from ILocator.
    //    /// </summary>
    //    public ICollection<Type> SupportedLocatorTypes { get; }

    //    Task<ICollection<IEntity>> ListChildEntitiesAsync(
    //        ILocator locator,
    //        CancellationToken cancellationToken);
    //}

    ////TODO: Strongly type. Return nullable?

    //// InstanceState, Icon, InstanceDetails, ConnectionSettings
    //// ConectionState (observable)
    //public interface IEntityAspectProvider
    //{
    //    public ICollection<Type> SupportedLocatorTypes { get; }
    //    public ICollection<Type> SupportedAspects { get; }
    //}

    //public interface IEntityAspectProvider<TAspect> : IEntityAspectProvider
    //{
    //    Task<TAspect> GetAspectAsync(
    //        ILocator locator,
    //        CancellationToken cancellationToken);
    //}

    public interface ICachingEntityProvider : IEntityContainer
    {
        /// <summary>
        /// Remove item from cache (if present) and cause it to
        /// be reloaded the next time it's accessed.
        /// </summary>
        void InvalidateItem(ILocator locator);
    }
}
