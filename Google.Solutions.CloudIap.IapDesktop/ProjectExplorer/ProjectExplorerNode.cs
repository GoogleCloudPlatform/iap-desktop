using Google.Apis.Compute.v1.Data;
using Google.Solutions.CloudIap.IapDesktop.Application.Registry;
using Google.Solutions.CloudIap.IapDesktop.Application.Settings;
using Google.Solutions.CloudIap.IapDesktop.Settings;
using Google.Solutions.Compute;
using Google.Solutions.IapDesktop.Application.Adapters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Google.Solutions.CloudIap.IapDesktop.Settings.SettingsEditorWindow;

namespace Google.Solutions.CloudIap.IapDesktop.ProjectExplorer
{
    internal class CloudNode : TreeNode, IProjectExplorerCloudNode
    {
        private const int IconIndex = 0;

        public CloudNode()
            : base("Google Cloud", IconIndex, IconIndex)
        {
        }
    }

    internal abstract class InventoryNode : TreeNode, IProjectExplorerNode, ISettingsObject
    {
        private readonly InventoryNode parent;
        private readonly InventorySettingsBase settings;

        public InventoryNode(string name, int iconIndex, InventorySettingsBase settings, InventoryNode parent)
            : base(name, iconIndex, iconIndex)
        {
            this.settings = settings;
            this.parent = parent;
        }

        public void SaveChanges()
        {
            
        }

        //---------------------------------------------------------------------
        // PropertyGrid-compatible settings properties.
        //---------------------------------------------------------------------

        [Browsable(true)]
        [BrowsableSetting]
        [Category("Credentials")]
        [DisplayName("Username")]
        [Description("Windows logon username")]
        public string Username
        {
            get => this.settings.Username ?? this.parent?.Username;
            set => this.settings.Username = value;
        }

        public bool ShouldSerializeUsername() => this.settings.Username != null;
                     

        [Browsable(true)]
        [BrowsableSetting]
        [Category("Credentials")]
        [DisplayName("Password")]
        [Description("Windows logon password")]
        [PasswordPropertyText(true)]
        public string Password
        {
            get => ShouldSerializePassword()
                ? new string('*', this.settings.Password.Length)
                : this.parent?.Password;
            set => this.settings.Password = SecureStringExtensions.FromClearText(value);
        }

        public bool ShouldSerializePassword() => this.settings.Password != null;


        [Browsable(true)]
        [BrowsableSetting]
        [Category("Credentials")]
        [DisplayName("Domain")]
        [Description("Windows logon domain")]
        public string Domain
        {
            get => this.settings.Domain ?? this.parent?.Domain;
            set => this.settings.Domain = value;
        }

        public bool ShouldSerializeDomain() => this.settings.Domain != null;


        [Browsable(true)]
        [BrowsableSetting]
        [Category("Display")]
        [DisplayName("Show connection bar")]
        [Description("Show connection bar in full-screen mode")]
        public RdpConnectionBarState ConnectionBar
        {
            get => ShouldSerializeConnectionBar()
                ? this.settings.ConnectionBar
                : (this.parent != null ? this.parent.ConnectionBar : RdpConnectionBarState._Default);
            set => this.settings.ConnectionBar = value;
        }

        public bool ShouldSerializeConnectionBar()
            => this.settings.ConnectionBar != RdpConnectionBarState._Default;


        [Browsable(true)]
        [BrowsableSetting]
        [Category("Display")]
        [DisplayName("Desktop size")]
        [Description("Size of remote desktop")]
        public RdpDesktopSize DesktopSize
        {
            get => ShouldSerializeDesktopSize()
                ? this.settings.DesktopSize
                : (this.parent != null ? this.parent.DesktopSize : RdpDesktopSize._Default);
            set => this.settings.DesktopSize = value;
        }

        public bool ShouldSerializeDesktopSize()
            => this.settings.DesktopSize != RdpDesktopSize._Default;


        [Browsable(true)]
        [BrowsableSetting]
        [Category("Display")]
        [DisplayName("Color depth")]
        [Description("Color depth of remote desktop")]
        public RdpColorDepth ColorDepth
        {
            get => ShouldSerializeColorDepth()
                ? this.settings.ColorDepth
                : (this.parent != null ? this.parent.ColorDepth : RdpColorDepth._Default);
            set => this.settings.ColorDepth = value;
        }

        public bool ShouldSerializeColorDepth()
            => this.settings.ColorDepth != RdpColorDepth._Default;


        [Browsable(true)]
        [BrowsableSetting]
        [Category("Connection")]
        [DisplayName("Server authentication")]
        [Description("Require server authentication when connecting")]
        public RdpAuthenticationLevel AuthenticationLevel
        {
            get => ShouldSerializeAuthenticationLevel()
                ? this.settings.AuthenticationLevel
                : (this.parent != null ? this.parent.AuthenticationLevel : RdpAuthenticationLevel._Default);
            set => this.settings.AuthenticationLevel = value;
        }

        public bool ShouldSerializeAuthenticationLevel()
            => this.settings.AuthenticationLevel != RdpAuthenticationLevel._Default;


        [Browsable(true)]
        [BrowsableSetting]
        [Category("Local resources")]
        [DisplayName("Redirect clipboard")]
        [Description("Allow clipboard contents to be shared with remote desktop")]
        public bool RedirectClipboard
        {
            get => ShouldSerializeRedirectClipboard()
                ? this.settings.RedirectClipboard
                : (this.parent != null ? this.parent.RedirectClipboard : true);
            set => this.settings.RedirectClipboard = value;
        }

        public bool ShouldSerializeRedirectClipboard()
            => !this.settings.RedirectClipboard;


        [Browsable(true)]
        [BrowsableSetting]
        [Category("Local resources")]
        [DisplayName("Audio mode")]
        [Description("Redirect audio when playing on server")]
        public RdpAudioMode AudioMode
        {
            get => ShouldSerializeAudioMode()
                ? this.settings.AudioMode
                : (this.parent != null ? this.parent.AudioMode : RdpAudioMode._Default);
            set => this.settings.AudioMode = value;
        }

        public bool ShouldSerializeAudioMode()
            => this.settings.AudioMode != RdpAudioMode._Default;
    }

    internal class ProjectNode : InventoryNode, IProjectExplorerProjectNode
    {
        private const int IconIndex = 1;

        private readonly InventorySettingsRepository settingsRepository;

        public string ProjectId => this.Text;

        public ProjectNode(InventorySettingsRepository settingsRepository, string projectId)
            : base(
                  projectId, 
                  IconIndex,
                  settingsRepository.GetProjectSettings(projectId),
                  null)
        {
            this.settingsRepository = settingsRepository;
        }

        private string longZoneToShortZoneId(string zone) => zone.Substring(zone.LastIndexOf("/") + 1);

        public void Populate(IEnumerable<Instance> allInstances)
        {
            this.Nodes.Clear();

            // Narrow the list down to Windows instances - there is no point 
            // of adding Linux instanes to the list of servers.
            var instances = allInstances.Where(i => ComputeEngineAdapter.IsWindowsInstance(i));
            var zoneIds = instances.Select(i => longZoneToShortZoneId(i.Zone)).ToHashSet();

            foreach (var zoneId in zoneIds)
            {
                var zoneSettings = this.settingsRepository.GetZoneSettings(
                    this.ProjectId, 
                    zoneId);
                var zoneNode = new ZoneNode(zoneSettings, this);

                var instancesInZone = instances
                    .Where(i => longZoneToShortZoneId(i.Zone) == zoneId)
                    .OrderBy(i => i.Name)
                    .Select(i => i.Name);

                foreach (var instanceName in instancesInZone)
                {
                    var instanceSettings = this.settingsRepository.GetVmInstanceSettings(
                        this.ProjectId, 
                        instanceName);
                    var instanceNode = new VmInstanceNode(instanceSettings, zoneNode);

                    zoneNode.Nodes.Add(instanceNode);
                }

                this.Nodes.Add(zoneNode);
                zoneNode.Expand();
            }

            Expand();
        }
    }

    internal class ZoneNode : InventoryNode, IProjectExplorerZoneNode
    {
        private const int IconIndex = 3;

        public string ProjectId => ((ProjectNode)this.Parent).ProjectId;
        public string ZoneId => this.Text;

        public ZoneNode(ZoneSettings settings, ProjectNode parent)
            : base(
                  settings.ZoneId, 
                  IconIndex,
                  settings,
                  parent)
        {
        }
    }

    internal class VmInstanceNode : InventoryNode, IProjectExplorerVmInstanceNode
    {
        private const int IconIndex = 4;
        private const int ActiveIconIndex = 4;

        public string ProjectId => ((ZoneNode)this.Parent).ProjectId;
        public string ZoneId => ((ZoneNode)this.Parent).ZoneId;

        public VmInstanceNode(VmInstanceSettings settings, ZoneNode parent)
            : base(
                  settings.InstanceName, 
                  IconIndex,
                  settings,
                  parent)
        {
        }

        [Browsable(true)]
        [BrowsableSetting]
        [Category("Instance")]
        [DisplayName("Name")]
        public string InstanceName => this.Text;
    }
}
