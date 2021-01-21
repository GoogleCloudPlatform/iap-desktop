using Google.Solutions.Common.Locator;
using Google.Solutions.Ssh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Services.Auth
{
    interface IPublicKeyService
    {
        Task PushPublicKeyAsync(
            InstanceLocator instance,
            string username,
            ISshKey key,
            CancellationToken token);
    }
}
