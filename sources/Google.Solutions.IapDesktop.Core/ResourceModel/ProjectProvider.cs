using Google.Solutions.Apis.Locator;
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
    internal class ProjectProvider : IResourceProvider
    {
        public Task<IResourceItemDetails> GetItemDetailsAsync(ILocator locator, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ICollection<IResourceItem>> ListItemsAsync(ILocator locator, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public bool CanHaveChildItems(ILocator locator)
        {
            throw new NotImplementedException();
        }
    }
}
