using Google.Apis.Compute.v1.Data;
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

        public string ProjectId => this.Text;

        public ProjectNode(string projectId)
            : base(projectId, IconIndex)
        {
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
                var zoneNode = new ZoneNode(zoneId);

                var instancesInZone = instances
                    .Where(i => longZoneToShortZoneId(i.Zone) == zoneId)
                    .OrderBy(i => i.Name)
                    .Select(i => i.Name);

                foreach (var instanceName in instancesInZone)
                {
                    var instanceNode = new VmInstanceNode(instanceName);

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

        public string ProjectId => ((ProjectNode)this.Parent).ProjectId;
        public string ZoneId => this.Text;

        public ZoneNode(string zoneId)
            : base(zoneId, IconIndex)
        {
        }
    }

    internal class VmInstanceNode : ProjectExplorerNode, IProjectExplorerVmInstanceNode
    {
        private const int IconIndex = 4;
        private const int ActiveIconIndex = 4;

        public string ProjectId => ((ZoneNode)this.Parent).ProjectId;
        public string ZoneId => ((ZoneNode)this.Parent).ZoneId;
        public string InstanceName => this.Text;

        public VmInstanceNode(string instanceName)
            : base(instanceName, IconIndex)
        {
        }
    }
}
