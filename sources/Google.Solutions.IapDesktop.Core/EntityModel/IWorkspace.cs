using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.ResourceModel
{
    /// <summary>
    /// Provides access to entities and their descendents.
    /// </summary>
    public interface IWorkspace : ISearchableEntityContainer<ILocator, IEntity>
    {
        /// <summary>
        /// Set the active/selected item.
        /// </summary>
        // Task SetActiveItemAsync(ILocator locator);

        /// <summary>
        /// Get the active/selected item
        /// </summary>
        // Task<ILocator> GetActiveItemAsync();

        /// <summary>
        /// Check if this item might have child items.
        /// </summary>
        bool IsContainer(ILocator locator); // -> check if any provider offers to list

        bool HasAspect<TAspect>(ILocator locator);



        /// <summary>
        /// List entities that are direct descendents of the entity 
        /// identified by the locator.
        /// </summary>
        //Task<ICollection<IEntity>> ListAsync(
        //    ILocator locator,
        //    CancellationToken cancellationToken);

        Task<TAspect> GetAsync<TAspect>(
            ILocator locator,
            CancellationToken cancellationToken);


        //Task<ICollection<IEntity>> SearchAsync(
        //    string query,
        //    CancellationToken cancellationToken);
    }
}
