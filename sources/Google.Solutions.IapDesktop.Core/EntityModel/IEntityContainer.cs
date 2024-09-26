using Google.Solutions.Apis.Locator;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.EntityModel
{
    /// <summary>
    /// Base interface for containers.
    /// </summary>
    /// <remarks>
    /// Implementing types must also implement
    /// the generic version of this interface.
    /// </remarks>
    public interface IEntityContainer
    {
    }

    public interface IEntityContainer<TLocator, TEntity> : IEntityContainer
        where TLocator : ILocator
        where TEntity : IEntity
    {
        Task<ICollection<TEntity>> ListAsync(
            TLocator locator,
            CancellationToken cancellationToken);

        Task DeleteAsync(
            TLocator locator,
            CancellationToken cancellationToken);
    }

    public interface IEntitySearcher
    {

    }

    public interface IEntitySearcher<TLocator, TEntity> : IEntitySearcher
        where TLocator : ILocator
        where TEntity : IEntity
    {
        Task<ICollection<TEntity>> SearchAsync(
            string query,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Base interface for aspect providers.
    /// </summary>
    /// <remarks>
    /// Implementing types must also implement
    /// the generic version of this interface.
    /// </remarks>
    public interface IEntityAspectProvider
    {
    }

    public interface IAsyncEntityAspectProvider<TLocator, TAspect> : IEntityAspectProvider
        where TLocator : ILocator
        where TAspect : class
    {
        Task<TAspect?> QueryAspectAsync(
            TLocator locator,
            CancellationToken cancellationToken);
    }


    public interface IEntityAspectProvider<TLocator, TAspect> : IEntityAspectProvider
        where TLocator : ILocator
        where TAspect : class
    {
        TAspect? QueryAspect(TLocator locator);
    }


    public interface ICachingEntityProvider : IEntityContainer
    {
        /// <summary>
        /// Remove item from cache (if present) and cause it to
        /// be reloaded the next time it's accessed.
        /// </summary>
        void InvalidateItem(ILocator locator);
    }
}
