using Google.Solutions.Apis.Locator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.EntityModel
{


    /// <summary>
    /// Indicates that a requested entity was not found.
    /// </summary>
    public abstract class EntityException : Exception
    {
        public ILocator Locator { get; }

        protected EntityException(ILocator locator)
        {
            this.Locator = locator;
        }
    }

    /// <summary>
    /// Indicates that a requested entity was not found.
    /// </summary>
    public class EntityNotFoundException : EntityException
    {
        public EntityNotFoundException(ILocator locator) : base(locator)
        {
        }
    }

    /// <summary>
    /// Indicates that a requested aspect was not found.
    /// </summary>
    public class EntityAspectNotFoundException : EntityException
    {
        public EntityAspectNotFoundException(ILocator locator) : base(locator)
        {
        }
    }
}
