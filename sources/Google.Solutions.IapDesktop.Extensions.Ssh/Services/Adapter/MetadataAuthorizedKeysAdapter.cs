using Google.Apis.Compute.v1.Data;
using Google.Solutions.Common.ApiExtensions.Instance;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
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
        Task PushAuthorizedKeySetToProjectMetadataAsync(
            InstanceLocator instance,
            MetadataAuthorizedKey newKey,
            CancellationToken token);

        Task PushAuthorizedKeySetToInstanceMetadataAsync(
            InstanceLocator instance,
            MetadataAuthorizedKey newKey,
            CancellationToken token);
    }

    public class MetadataAuthorizedKeysAdapter : IMetadataAuthorizedKeysAdapter
    {
        private readonly IComputeEngineAdapter computeEngineAdapter;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public MetadataAuthorizedKeysAdapter(
            IComputeEngineAdapter computeEngineAdapter)
        {
            this.computeEngineAdapter = computeEngineAdapter;
        }

        public MetadataAuthorizedKeysAdapter(IServiceProvider serviceProvider)
            : this(serviceProvider.GetService<IComputeEngineAdapter>())
        {
        }

        //---------------------------------------------------------------------
        // IMetadataAuthorizedKeysAdapter.
        //---------------------------------------------------------------------

        public Task PushAuthorizedKeySetToProjectMetadataAsync(
            InstanceLocator instance,
            MetadataAuthorizedKey newKey,
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(instance))
            {
                throw new NotImplementedException();
            }
        }

        public async Task PushAuthorizedKeySetToInstanceMetadataAsync(
            InstanceLocator instance,
            MetadataAuthorizedKey newKey,
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(instance))
            {
                await this.computeEngineAdapter.UpdateMetadataAsync(
                        instance,
                        metadata =>
                        {
                            //
                            // Merge new key into existing keyset, and take 
                            // the opportunity to purge expired keys.
                            //
                            var newKeySet = MetadataAuthorizedKeySet.FromMetadata(metadata)
                                .RemoveExpiredKeys()
                                .Add(newKey);
                            metadata.Add(MetadataAuthorizedKeySet.MetadataKey, newKeySet.ToString());
                        },
                        token)
                    .ConfigureAwait(false);
            }
        }
    }
}
