using Google.Solutions.Apis.Locator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.ResourceModel
{
    public interface IResourceItemProvider
    {
        /// <summary>
        /// Types of locators that this provider supports.
        /// Types must be derived from ILocator.
        /// </summary>
        public ICollection<Type> SupportedLocatorTypes { get; }

        /// <summary>
        /// List child items and return basic information about
        /// each item.
        /// </summary>
        Task<ICollection<IResourceItem>> ListItemsAsync(
            ILocator locator,
            CancellationToken cancellationToken);

        /// <summary>
        /// Check if this item might have child items.
        /// </summary>
        bool CanHaveChildItems(ILocator locator);
    }

    public interface IResourceItemDetailsProvider
    {
        /// <summary>
        /// Lookup the full details for a resource item.
        /// </summary>
        /// <returns>List of items. Items might not all be of the same type.</returns>
        Task<IResourceItemDetails> GetItemDetailsAsync(
            ILocator locator,
            Type type,
            CancellationToken cancellationToken);
    }

    public interface ISearchableResourceProvider : IResourceItemProvider
    {
        Task<ICollection<IResourceItem>> SearchItemsAsync(
            string query,
            CancellationToken cancellationToken);
    }

    public interface ICachingResourceProvider : IResourceItemProvider
    {
        /// <summary>
        /// Remove item from cache (if present) and cause it to
        /// be reloaded the next time it's accessed.
        /// </summary>
        void Invalidate(ILocator locator);
    }
}
