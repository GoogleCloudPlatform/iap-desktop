using Google.Apis.CloudResourceManager.v1.Data;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Util;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Google.Solutions.IapDesktop.Application.Views.ProjectPicker
{
    public sealed class ProjectPickerViewModel : ViewModelBase,  IDisposable
    {
        private const int MaxResults = 100;

        private readonly IResourceManagerAdapter resourceManager;

        private Project selectedProject;
        private string filter;
        private string statusText;
        private bool isLoading;
        private Exception filteringException;

        public ProjectPickerViewModel(
            IResourceManagerAdapter resourceManager)
        {
            this.resourceManager = resourceManager;
        }

        //---------------------------------------------------------------------
        // Observable "output" properties.
        //---------------------------------------------------------------------

        public RangeObservableCollection<Project> FilteredProjects { get; }
            = new RangeObservableCollection<Project>();

        public bool IsLoading
        {
            get => this.isLoading;
            private set
            {
                this.isLoading = value;
                RaisePropertyChange();
            }
        }
        
        public bool IsProjectSelected => this.selectedProject != null;

        public Project SelectedProject
        {
            get => this.selectedProject;
            set
            {
                this.selectedProject = value;
                RaisePropertyChange();
                RaisePropertyChange((ProjectPickerViewModel m) => m.IsProjectSelected);
            }
        }

        public Exception LoadingError
        {
            get => this.filteringException;
            private set
            {
                this.filteringException = value;
                RaisePropertyChange();
            }
        }

        public string StatusText
        {
            get => this.statusText;
            private set
            {
                this.statusText = value;
                RaisePropertyChange();
                RaisePropertyChange((ProjectPickerViewModel m) => m.IsStatusTextVisible);
            }
        }

        public bool IsStatusTextVisible
        {
            get => this.statusText != null;
        }

        //---------------------------------------------------------------------
        // "Input" properties.
        //---------------------------------------------------------------------

        public string Filter
        {
            get => this.filter;
            set
            {
                FilterAsync(value).ContinueWith(t => { });
            }
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public async Task FilterAsync(string filter)
        {
            //
            // Update property synchrounously.
            //
            this.filter = filter;
            RaisePropertyChange((ProjectPickerViewModel m) => m.Filter);

            this.IsLoading = true;
            this.SelectedProject = null;
            this.FilteredProjects.Clear();

            //
            // Start server-side search asynchronously, then 
            // update remaining properties on original (UI) thread.
            //
            try
            {
                var result = await this.resourceManager.ListProjects(
                        string.IsNullOrEmpty(this.filter)
                            ? null // All projects.
                            : ProjectFilter.ByPrefix(this.filter),
                        MaxResults,
                        CancellationToken.None)
                    .ConfigureAwait(true);

                // Clear again because multiple filter operations might be running
                // in parallel.
                this.FilteredProjects.Clear();
                this.FilteredProjects.AddRange(result.Projects);
                if (result.IsTruncated)
                {
                    this.StatusText =
                        $"Over {result.Projects.Count()} projects found, " +
                            "use search to refine selection";
                }
                else
                {
                    this.StatusText =
                        $"{result.Projects.Count()} projects found";
                }
            }
            catch (Exception e)
            {
                this.LoadingError = e;
            }

            this.IsLoading = false;

            RaisePropertyChange((ProjectPickerViewModel m) => m.FilteredProjects);
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
