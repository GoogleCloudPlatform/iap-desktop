using Google.Apis.Compute.v1.Data;
using Google.Solutions.CloudIap.IapDesktop.Application.Settings;
using Google.Solutions.Compute;
using Google.Solutions.IapDesktop.Application.Adapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.CloudIap.IapDesktop.ProjectExplorer
{
    internal abstract class ProjectExplorerNode : TreeNode, IProjectExplorerNode
    {
        public ProjectExplorerNode(string name, int iconIndex)
            : base(name, iconIndex, iconIndex)
        {
        }
    }

    internal class CloudNode : ProjectExplorerNode, IProjectExplorerCloudNode
    {
        private const int IconIndex = 0;

        public CloudNode()
            : base("Google Cloud", IconIndex)
        {
        }
    }

    internal class ProjectNode : ProjectExplorerNode, IProjectExplorerProjectNode
    {
        private const int IconIndex = 1;

        private readonly InventorySettingsRepository settingsRepository;
        private readonly ProjectSettings settings;

        public string ProjectId => this.Text;

        public ProjectNode(InventorySettingsRepository settingsRepository, string projectId)
            : base(projectId, IconIndex)
        {
            this.settingsRepository = settingsRepository;
            this.settings = settingsRepository.GetProjectSettings(projectId);
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
                var zoneNode = new ZoneNode(zoneSettings);

                var instancesInZone = instances
                    .Where(i => longZoneToShortZoneId(i.Zone) == zoneId)
                    .OrderBy(i => i.Name)
                    .Select(i => i.Name);

                foreach (var instanceName in instancesInZone)
                {
                    var instanceSettings = this.settingsRepository.GetVmInstanceSettings(
                        this.ProjectId, 
                        instanceName);
                    var instanceNode = new VmInstanceNode(instanceSettings);

                    zoneNode.Nodes.Add(instanceNode);
                }

                this.Nodes.Add(zoneNode);
                zoneNode.Expand();
            }

            Expand();
        }
    }

    internal class ZoneNode : ProjectExplorerNode, IProjectExplorerZoneNode
    {
        private const int IconIndex = 3;

        private readonly ZoneSettings settings;

        public string ProjectId => ((ProjectNode)this.Parent).ProjectId;
        public string ZoneId => this.Text;

        public ZoneNode(ZoneSettings settings)
            : base(settings.ZoneId, IconIndex)
        {
            this.settings = settings;
        }
    }

    internal class VmInstanceNode : ProjectExplorerNode, IProjectExplorerVmInstanceNode
    {
        private const int IconIndex = 4;
        private const int ActiveIconIndex = 4;

        private readonly VmInstanceSettings settings;

        public string ProjectId => ((ZoneNode)this.Parent).ProjectId;
        public string ZoneId => ((ZoneNode)this.Parent).ZoneId;
        public string InstanceName => this.Text;

        public VmInstanceNode(VmInstanceSettings settings)
            : base(settings.InstanceName, IconIndex)
        {
            this.settings = settings;
        }
    }
}
