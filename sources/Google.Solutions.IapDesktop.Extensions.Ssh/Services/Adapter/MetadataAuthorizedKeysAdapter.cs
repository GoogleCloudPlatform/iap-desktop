using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.Ssh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Services.Adapter
{
    public interface IMetadataAuthorizedKeysAdapter
    {
        Task PushPublicKeyToProjectMetadataAsync(
            InstanceLocator instance,
            string username,
            ISshKey key,
            CancellationToken token);

        Task PushPublicKeyToInstanceMetadataAsync(
            InstanceLocator instance,
            string username,
            ISshKey key,
            CancellationToken token);
    }

    public class MetadataAuthorizedKeysAdapter : IMetadataAuthorizedKeysAdapter
    {

        public Task PushPublicKeyToProjectMetadataAsync(
            InstanceLocator instance,
            string username,
            ISshKey key,
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(instance))
            {
                throw new NotImplementedException();
            }
        }

        public Task PushPublicKeyToInstanceMetadataAsync(
            InstanceLocator instance,
            string username,
            ISshKey key,
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(instance))
            {
                throw new NotImplementedException();
            }
        }
    }
}
