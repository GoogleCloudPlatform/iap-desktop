using Google.Solutions.Apis.Locator;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.EntityModel
{
    /// <summary>
    /// Base interface for expanders.
    /// </summary>
    /// <remarks>
    /// Implementing types must also implement
    /// the generic version of this interface.
    /// </remarks>
    public interface IEntityExpander
    {
    }

    /// <summary>
    /// Discover child entities.
    /// </summary>
    /// <typeparam name="TLocator">Parent locator type</typeparam>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TEntityLocator">Entity locator type</typeparam>
    public interface IEntityExpander<TLocator, TEntity, TEntityLocator> : IEntityExpander
        where TLocator : ILocator
        where TEntityLocator: ILocator
        where TEntity : IEntity<TEntityLocator>
    {
        /// <summary>
        /// List direct descendents.
        /// </summary>
        Task<ICollection<TEntity>> ExpandAsync(
            TLocator parent,
            CancellationToken cancellationToken);

        /// <summary>
        /// Invalidate cache.
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
