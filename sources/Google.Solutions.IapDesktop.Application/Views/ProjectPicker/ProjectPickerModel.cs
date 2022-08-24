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
            string prefix,
            int maxResults,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Picker for all accessible projects.
    /// </summary>
    public sealed class AccessibleProjectPickerModel : IProjectPickerModel
    {
        private readonly IResourceManagerAdapter resourceManager;

        public AccessibleProjectPickerModel(
            IResourceManagerAdapter resourceManager)
        {
            this.resourceManager = resourceManager;
        }

        //---------------------------------------------------------------------
        // IProjectPickerModel.
        //---------------------------------------------------------------------

        public async Task<FilteredProjectList> ListProjectsAsync(
            string prefix,
            int maxResults,
            CancellationToken cancellationToken)
        {
            return await this.resourceManager.ListProjectsAsync(
                    string.IsNullOrEmpty(prefix)
                        ? null // All projects.
                        : ProjectFilter.ByPrefix(prefix),
                    maxResults,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public void Dispose()
        {
            this.resourceManager.Dispose();
        }
    }

    /// <summary>
    /// Picker for a static set of projects.
    /// </summary>
    public class StaticProjectPickerModel : IProjectPickerModel
    {
        private readonly IReadOnlyCollection<Project> projects;

        public StaticProjectPickerModel(IReadOnlyCollection<Project> projects)
        {
            this.projects = projects;
        }

        //---------------------------------------------------------------------
        // IProjectPickerModel.
        //---------------------------------------------------------------------

        public Task<FilteredProjectList> ListProjectsAsync(
            string prefix,
            int maxResults,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                new FilteredProjectList(
                    this.projects
                        .Where(p => prefix == null ||
                                    p.Name.StartsWith(prefix) ||
                                    p.ProjectId.StartsWith(prefix))
                        .Take(maxResults),
                    false));
        }

        public void Dispose()
        {
        }
    }
}
