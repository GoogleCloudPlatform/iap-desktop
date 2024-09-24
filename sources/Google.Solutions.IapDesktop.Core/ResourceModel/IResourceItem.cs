using Google.Solutions.Apis.Locator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.ResourceModel
{
    /// <summary>
    /// Summary information about a resource.
    /// </summary>
    public interface IResourceItem
    {
        /// <summary>
        /// Display name, might differ from the name
        /// used in the locator.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Locator for this item.
        /// </summary>
        ILocator Locator { get; }
    }

    /// <summary>
    /// Detailed information about a resource.
    /// </summary>
    public interface IResourceItemDetails : IResourceItem
    {
    }
}
