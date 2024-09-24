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
    [ServiceCategory(typeof(IResourceItemProvider))]
    internal class ProjectProvider : IResourceItemProvider, IResourceItemDetailsProvider
    {
        private static bool IsSupportedLocator(ILocator locator)
        {
            return locator is UniverseLocator || locator is OrganizationLocator ;
        }

        public void AddProject(ProjectLocator project)
        {

        }

        public void RemoveProject(ProjectLocator project)
        {

        }

        //----------------------------------------------------------------------
        // IResourceProvider.
        //----------------------------------------------------------------------

        public ICollection<Type> SupportedLocatorTypes
        {
            get => new[] 
            { 
                typeof(UniverseLocator),
                typeof(OrganizationLocator) 
            };
        }

        public Task<ICollection<IResourceItem>> ListItemsAsync(
            ILocator locator, 
            CancellationToken cancellationToken)
        {
            if (locator is UniverseLocator universeLocator)
            {
                // List organizations.
            }
            else if (locator is OrganizationLocator organizationLocator)
            {
                // List projects.
            }
            else
            {
                throw new ArgumentException("The locator type is not supported");
            }
        }

        public bool CanHaveChildItems(ILocator locator)
        {
            Precondition.Expect(IsSupportedLocator(locator), "Valid locator");
            return true;
        }

        //----------------------------------------------------------------------
        // IResourceItemDetailsProvider.
        //----------------------------------------------------------------------

        public Task<IResourceItemDetails> GetItemDetailsAsync(
            ILocator locator, 
            Type type,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
