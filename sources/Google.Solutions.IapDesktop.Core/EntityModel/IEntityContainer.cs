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

    /// <summary>
    /// Container foe entities.
    /// </summary>
    /// <typeparam name="TLocator">Parent locator type</typeparam>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TEntityLocator">Entity locator type</typeparam>
    public interface IEntityContainer<TLocator, TEntity, TEntityLocator> : IEntityContainer // TODO: Rename to IEntityNavigator?
        where TLocator : ILocator
        where TEntityLocator: ILocator
        where TEntity : IEntity<TEntityLocator>
    {
        /// <summary>
        /// List direct descendents.
        /// </summary>
        Task<ICollection<TEntity>> ListAsync(
            TLocator parent,
            CancellationToken cancellationToken);

        /// <summary>
        /// Invalidate the container.
        /// </summary>
        void Invalidate(TLocator locator);

        /// <summary>
        /// Delete an entity.
        /// </summary>
        //Task DeleteAsync(
        //    TEntityLocator entity,
        //    CancellationToken cancellation);
    }
}
