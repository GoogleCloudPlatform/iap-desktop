using Google.Solutions.Apis.Locator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.EntityModel
{
    public interface IEntityCache
    {
    }

    public interface IEntityCache<TLocator>
        where TLocator : ILocator
    {
        /// <summary>
        /// Invalidate cache.
        /// </summary>
        void Invalidate(TLocator locator);
    }
}
