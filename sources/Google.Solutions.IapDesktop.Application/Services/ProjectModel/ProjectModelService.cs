using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services.ProjectModel
{
    public interface IProjectModelService
    {
        Task AddProjectAsync(ProjectLocator project);

        Task RemoveProjectAsync(ProjectLocator project);

        IProjectExplorerCloudNode GetModel(
            OperatingSystems operatingSystems);
    }

    public class ProjectModelService : IProjectModelService
    {
        private readonly IProjectRepository repository;
        private readonly IEventService eventService;

        public ProjectModelService(
            IProjectRepository repository,
            IEventService eventService)
        {
            this.repository = repository;
            this.eventService = eventService;
        }

        public ProjectModelService(IServiceProvider serviceProvider)
            : this(
                  serviceProvider.GetService<IProjectRepository>(),
                  serviceProvider.GetService<IEventService>())
        {
        }

        //---------------------------------------------------------------------
        // IProjectModelService.
        //---------------------------------------------------------------------

        public async Task AddProjectAsync(ProjectLocator project)
        {
            await this.repository
                .AddProjectAsync(project.ProjectId)
                .ConfigureAwait(false);

            await this.eventService
                .FireAsync(new ProjectDeletedEvent(project.ProjectId))
                .ConfigureAwait(false);
        }

        public async Task RemoveProjectAsync(ProjectLocator project)
        {
            await this.repository
                .DeleteProjectAsync(project.ProjectId)
                .ConfigureAwait(false);

            await this.eventService
                .FireAsync(new ProjectDeletedEvent(project.ProjectId))
                .ConfigureAwait(false);
        }

        public IProjectExplorerCloudNode GetModel(
            OperatingSystems operatingSystems)
        {
            throw new NotImplementedException();
        }

    }
}
