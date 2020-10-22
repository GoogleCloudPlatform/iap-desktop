using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Util;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.Options
{
    internal class GeneralOptionsViewModel : ViewModelBase, IOptionsDialogPane
    {
        internal const string FriendlyName = "IAP Desktop - Identity-Aware Proxy for Remote Desktop";

        private readonly ApplicationSettingsRepository settingsRepository;
        private readonly ApplicationSettings settings;
        private readonly AppProtocolRegistry protocolRegistry;

        private bool isBrowserIntegrationEnabled;
        private bool isDirty = false;

        public GeneralOptionsViewModel(IServiceProvider serviceProvider)
        {
            this.settingsRepository = serviceProvider.GetService<ApplicationSettingsRepository>();
            this.settings = this.settingsRepository.GetSettings();

            this.protocolRegistry = serviceProvider.GetService<AppProtocolRegistry>();
            this.isBrowserIntegrationEnabled = this.protocolRegistry.IsRegistered(
                IapRdpUrl.Scheme,
                Assembly.GetEntryAssembly().Location);
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public string Title => "General";

        public bool IsDirty
        {
            get => this.isDirty;
            set
            {
                this.isDirty = value;
                RaisePropertyChange();
            }
        }

        public bool IsUpdateCheckEnabled
        {
            get => this.settings.IsUpdateCheckEnabled.BoolValue;
            set
            {
                this.settings.IsUpdateCheckEnabled.BoolValue = value;
                this.IsDirty = true;
                RaisePropertyChange();
            }
        }

        public bool IsBrowserIntegrationEnabled
        {
            get => this.isBrowserIntegrationEnabled;
            set
            {
                this.isBrowserIntegrationEnabled = value;
                this.IsDirty = true;
                RaisePropertyChange();
            }
        }

        public string LastUpdateCheck => this.settings.LastUpdateCheck.IsDefault
            ? "never"
            : DateTime.FromBinary(this.settings.LastUpdateCheck.LongValue).ToString();

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public void ApplyChanges()
        {
            // Save changed settings.
            this.settingsRepository.SetSettings(this.settings);

            // Update protocol registration.
            if (this.isBrowserIntegrationEnabled)
            {
                this.protocolRegistry.Register(
                    IapRdpUrl.Scheme,
                    FriendlyName,
                    Assembly.GetEntryAssembly().Location);
            }
            else
            {
                this.protocolRegistry.Unregister(IapRdpUrl.Scheme);
            }

            this.IsDirty = false;
        }

        public UserControl CreateControl() => new GeneralOptionsControl(this);
    }
}
