using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.SshKeys
{
    public class MetadataAuthorizedPublicKeysViewModel
        : ModelCachingViewModelBase<IProjectModelNode, AuthorizedPublicKeysModel>
    {
        private const int ModelCacheCapacity = 5;
        private const string WindowTitlePrefix = "Metadata authorized SSH keys";

        private readonly IServiceProvider serviceProvider;

        private bool isLoading;
        private string windowTitle;
        private string informationBarContent;
        private KeyAuthorizationMethods authorizationMethods;

        public MetadataAuthorizedPublicKeysViewModel(
            IServiceProvider serviceProvider)
            : base(ModelCacheCapacity)
        {
            this.serviceProvider = serviceProvider;
        }

        //---------------------------------------------------------------------
        // Observable "output" properties.
        //---------------------------------------------------------------------

        public RangeObservableCollection<AuthorizedPublicKeysModel.Item> Items { get; }

        public bool IsLoading
        {
            get => this.isLoading;
            set
            {
                this.isLoading = value;
                RaisePropertyChange();
            }
        }

        public string WindowTitle
        {
            get => this.windowTitle;
            set
            {
                this.windowTitle = value;
                RaisePropertyChange();
            }
        }

        public bool IsOsLoginKeysEnabled
        {
            get => this.authorizationMethods.HasFlag(KeyAuthorizationMethods.Oslogin);
            set
            {
                this.authorizationMethods |= KeyAuthorizationMethods.Oslogin;
                RaisePropertyChange();
            }
        }

        public bool IsProjectMetadataKeysEnabled
        {
            get => this.authorizationMethods.HasFlag(KeyAuthorizationMethods.ProjectMetadata);
            set
            {
                this.authorizationMethods |= KeyAuthorizationMethods.ProjectMetadata;
                RaisePropertyChange();
            }
        }

        public bool IsProjectInstanceKeysEnabled
        {
            get => this.authorizationMethods.HasFlag(KeyAuthorizationMethods.InstanceMetadata);
            set
            {
                this.authorizationMethods |= KeyAuthorizationMethods.InstanceMetadata;
                RaisePropertyChange();
            }
        }

        public bool IsInformationBarVisible => this.InformationBarContent != null;

        public string InformationBarContent
        {
            get => this.informationBarContent;
            private set
            {
                this.informationBarContent = value;
                RaisePropertyChange();
                RaisePropertyChange((MetadataAuthorizedPublicKeysViewModel m) => m.IsInformationBarVisible);
            }
        }

        //---------------------------------------------------------------------
        // Static helpers.
        //---------------------------------------------------------------------

        // TODO: test
        internal static CommandState GetCommandState(IProjectModelNode node)
        {
            return (node is IProjectModelInstanceNode ||
                    node is IProjectModelProjectNode)
                ? CommandState.Enabled
                : CommandState.Unavailable;
        }

        //---------------------------------------------------------------------
        // ModelCachingViewModelBase.
        //---------------------------------------------------------------------

        // TODO: test
        protected async override Task<AuthorizedPublicKeysModel> LoadModelAsync(
            IProjectModelNode node, 
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(node))
            {
                try
                {
                    this.IsLoading = true;

                    //
                    // Reset window title, otherwise the default or previous title
                    // stays while data is loading.
                    //
                    this.WindowTitle = WindowTitlePrefix;

                    var jobService = this.serviceProvider.GetService<IJobService>();

                    //
                    // Load data using a job so that the task is retried in case
                    // of authentication issues.
                    //
                    return await jobService.RunInBackground(
                        new JobDescription(
                            $"Loading inventory for {node.DisplayName}",
                            JobUserFeedbackType.BackgroundFeedback),
                        async jobToken =>
                        {
                            using (var computeEngineAdapter = this.serviceProvider.GetService<IComputeEngineAdapter>())
                            using (var resourceManagerAdapter = this.serviceProvider.GetService<IResourceManagerAdapter>())
                            using (var osLoginService = this.serviceProvider.GetService<IOsLoginService>())
                            {
                                return await AuthorizedPublicKeysModel.LoadAsync(
                                        computeEngineAdapter,
                                        resourceManagerAdapter,
                                        osLoginService,
                                        node,
                                        this.authorizationMethods,
                                        jobToken)
                                    .ConfigureAwait(false);
                            }
                        }).ConfigureAwait(true);  // Back to original (UI) thread.
                }
                finally
                {
                    this.IsLoading = false;
                }
            }
        }

        // TODO: test
        protected override void ApplyModel(bool cached)
        {
            this.Items.Clear();

            if (this.Model == null)
            {
                // Unsupported node.
                this.InformationBarContent = null;
                this.WindowTitle = WindowTitlePrefix;
            }
            else
            {
                this.InformationBarContent = this.Model.Warnings.Any()
                    ? string.Join(", ", this.Model.Warnings)
                    : null;
                this.WindowTitle = WindowTitlePrefix + $": {this.Model.DisplayName}";
                this.Items.AddRange(this.Model.Items);
            }
        }
    }
}
