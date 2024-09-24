using Google.Solutions.Apis.Locator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.ResourceModel
{

    public interface IWorkspace : ISearchableResourceProvider, ICachingResourceProvider
    {
        /// <summary>
        /// Add a project so that it will be considered when
        /// the model is next (force-) reloaded.
        /// </summary>
        Task AddProjectAsync(ProjectLocator project);

        /// <summary>
        /// Remove project so that it will not be considered when
        /// the model is next (force-) reloaded.
        /// </summary>
        Task RemoveProjectAsync(ProjectLocator project);

        /// <summary>
        /// Set the active/selected item.
        /// </summary>
        Task SetActiveItemAsync(ILocator locator);

        /// <summary>
        /// Get the active/selected item
        /// </summary>
        Task<ILocator> GetActiveItemAsync();

        void Mount(ILocator locator, IResourceProvider provider);
    }
}
