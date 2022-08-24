using Google.Apis.CloudResourceManager.v1.Data;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Views.ProjectPicker
{
    public interface IProjectPickerModel : IDisposable
    {
        Task<FilteredProjectList> ListProjectsAsync(
            string filter,
            int maxResults,
            CancellationToken cancellationToken);
    }

    public sealed class CloudProjectPickerModel : IProjectPickerModel
    {
        private readonly IResourceManagerAdapter resourceManager;

        public CloudProjectPickerModel(
            IResourceManagerAdapter resourceManager)
        {
            this.resourceManager = resourceManager;
        }

        //---------------------------------------------------------------------
        // IProjectPickerModel.
        //---------------------------------------------------------------------

        public async Task<FilteredProjectList> ListProjectsAsync(
            string filter,
            int maxResults,
            CancellationToken cancellationToken)
        {
            return await this.resourceManager.ListProjectsAsync(
                    string.IsNullOrEmpty(filter)
                        ? null // All projects.
                        : ProjectFilter.ByPrefix(filter),
                    maxResults,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            this.resourceManager.Dispose();
        }
    }
}
