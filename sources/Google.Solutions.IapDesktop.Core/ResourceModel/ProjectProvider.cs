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
    internal class ProjectProvider : IResourceItemProvider
    {

        private static bool IsSupportedLocator(ILocator locator)
        {
            return locator is OrganizationLocator
        }

        //----------------------------------------------------------------------
        // IResourceProvider.
        //----------------------------------------------------------------------

        public ICollection<Type> SupportedLocatorTypes
        {
            get => new[] 
            { 
                //typeof(UniverseLocator) 
                typeof(OrganizationLocator) 
            };
        }

        public Task<IResourceItemDetails> GetItemDetailsAsync(ILocator locator, CancellationToken cancellationToken)
        {
            //
            // For universes and organizations, there are no additional details to be
            // looked up.
            //
            if (locator is OrganizationLocator)
            {
            }
            

            Precondition.Expect(IsSupportedLocator(locator), "Valid locator");
            throw new NotImplementedException();
        }

        public Task<ICollection<IResourceItem>> ListItemsAsync(ILocator locator, CancellationToken cancellationToken)
        {
            Precondition.Expect(IsSupportedLocator(locator), "Valid locator");
            throw new NotImplementedException();
        }

        public bool CanHaveChildItems(ILocator locator)
        {
            Precondition.Expect(IsSupportedLocator(locator), "Valid locator");
            return true;
        }
    }
}
