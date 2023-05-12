using Google.Solutions.Apis.Locator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Transport
{
    public interface ITrait
    {
    }

    public class InstanceTrait
    {
        public InstanceLocator Instance { get; }
    }
}
