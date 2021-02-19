using Google.Apis.CloudResourceManager.v1.Data;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Google.Solutions.IapDesktop.Application.Views.ProjectPicker
{
    public sealed class ProjectPickerViewModel : ViewModelBase,  IDisposable
    {
        private const int MaxResults = 100;

        private readonly IResourceManagerAdapter resourceManager;

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

        
        public bool IsProjectSelected => false;

        public Project SelectedProject => null;

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
                this.filter = value;

                this.isLoading = true;
                this.FilteredProjects.Clear();

                //
                // Start server-side search, update view model
                // asynchronously, but on UI thread.
                //

                this.resourceManager.ListProjects(
                        string.IsNullOrEmpty(this.filter)
                            ? null // All projects.
                            : ProjectFilter.ByPrefix(value),
                        MaxResults,
                        CancellationToken.None)
                    .ContinueWith(t =>
                    {
                        try
                        {
                            this.FilteredProjects.AddRange(t.Result.Projects);
                            if (t.Result.IsTruncated)
                            {
                                this.StatusText = $"Over {t.Result.Projects.Count()} projects found, " +
                                                   "use search to refine selection";
                            }
                            else
                            {
                                this.StatusText = $"{t.Result.Projects.Count()} projects found";
                            }
                        }
                        catch (Exception e)
                        {
                            this.LoadingError = e;
                        }

                        this.isLoading = false;
                    },
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.FromCurrentSynchronizationContext()); // Continue on UI thread.

                RaisePropertyChange((ProjectPickerViewModel m) => m.FilteredProjects);
                RaisePropertyChange();
            }
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
