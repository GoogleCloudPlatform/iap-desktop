using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.Adapters;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Apis.Compute.v1.Data;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Application.Services
{
    /// <summary>
    /// This service manages the project inventory.
    /// </summary>
    public class ProjectInventoryService
    {
        private readonly InventorySettingsRepository inventorySettings;
        private readonly IEventService eventService;

        public ProjectInventoryService(
            InventorySettingsRepository inventorySettings,
            IEventService eventService)
        {
            this.inventorySettings = inventorySettings;
            this.eventService = eventService;
        }

        public ProjectInventoryService(IServiceProvider provider)
            : this(
                provider.GetService<InventorySettingsRepository>(),
                provider.GetService<IEventService>())
        {}

        public async Task AddProjectAsync(string projectId)
        {
            this.inventorySettings.SetProjectSettings(new ProjectSettings()
            {
                ProjectId = projectId
            });

            await this.eventService.FireAsync(new ProjectAddedEvent(projectId));
        }

        public async Task DeleteProjectAsync(string projectId)
        {
            this.inventorySettings.DeleteProjectSettings(projectId);

            await this.eventService.FireAsync(new ProjectDeletedEvent(projectId));
        }

        public Task<IEnumerable<Project>> ListProjectsAsync()
        {
            return Task.FromResult(this.inventorySettings
                .ListProjectSettings()
                .Select(s => new Project()
                {
                    Name = s.ProjectId
                }));
        }

        public class ProjectAddedEvent 
        {
            public string ProjectId { get; }

            public ProjectAddedEvent(string projectId)
            {
                this.ProjectId = projectId;
            }
        }

        public class ProjectDeletedEvent
        {
            public string ProjectId { get; }

            public ProjectDeletedEvent(string projectId)
            {
                this.ProjectId = projectId;
            }
        }
    }
}
