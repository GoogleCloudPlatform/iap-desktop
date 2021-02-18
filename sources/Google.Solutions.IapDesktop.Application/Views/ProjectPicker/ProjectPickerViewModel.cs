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
        private readonly IResourceManagerAdapter resourceManager;

        private string filter;
        private bool isLoading;
        private bool isListTruncated;
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

        public bool IsListTruncated
        {
            get => this.isListTruncated;
            private set
            {
                this.isListTruncated = value;
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

        //---------------------------------------------------------------------
        // "Input" properties.
        //---------------------------------------------------------------------

        public string Filter
        {
            get => this.filter;
            set
            {
                this.filter = value;

                if (string.IsNullOrWhiteSpace(this.filter))
                {
                    // TODO: Load all projects
                    return;
                }

                this.isLoading = true;
                this.FilteredProjects.Clear();

                //
                // Start server-side search, update view model
                // asynchronously, but on UI thread.
                //

                // TODO: Set limit

                this.resourceManager.QueryProjectsByPrefix(
                        value,
                        CancellationToken.None)
                    .ContinueWith(t =>
                    {
                        try
                        {
                            this.FilteredProjects.AddRange(t.Result);

                            // TODO: Set IsTruncated flag
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
