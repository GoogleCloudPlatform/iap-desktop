using Google.Apis.Logging.v2.Data;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.ResourceModel
{
    /// <summary>
    /// Provider for the following resources:
    /// - Organizations
    /// - Projects
    /// </summary>
    [ServiceCategory(typeof(IEntityContainer))]
    internal class ProjectProvider : IEntityContainer<ProjectLocator, ProjectEntity>, ICachingEntityProvider
    {
        private readonly IProjectProviderContext context;
    }

    internal class ProjectEntity : IEntity<ProjectLocator>
    {

    }

    interface IProjectProviderContext
    {
        ICollection<ProjectLocator> Projects { get; }


        /// <summary>
        /// Get project's ancestry, in top-to-bottom order.
        /// 
        /// The ancestry path might be incomplete or empty if the current 
        /// doesn't have sufficient access to resolve the full ancestry.
        /// </summary>
        /// <returns>false if ancestry hasn't been set before</returns>
        bool TryGetAncestry(ProjectLocator project, out IEnumerable<ILocator> ancestry);

        /// <summary>
        /// Save project ancestry path, in top-to-bottom order.
        /// 
        /// The ancestry path might be incomplete or empty if the current 
        /// doesn't have sufficient access to resolve the full ancestry.
        /// </summary>
        void SetAncestry(ProjectLocator project, out IEnumerable<ILocator> ancestry);


    }
}
