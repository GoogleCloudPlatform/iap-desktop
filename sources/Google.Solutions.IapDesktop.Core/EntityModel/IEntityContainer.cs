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
        /// <summary>
        /// List direct descendents.
        /// </summary>
        Task<ICollection<TEntity>> ListAsync(
            TLocator locator,
            CancellationToken cancellationToken);

        /// <summary>
        /// Delete item.
        /// </summary>
        Task DeleteAsync(
            TLocator locator,
            CancellationToken cancellationToken);

        /// <summary>
        /// Remove item from cache (if present) and cause it to
        /// be reloaded the next time it's accessed.
        /// </summary>
        void Invalidate(TLocator locator);
    }
}
