using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.CloudIap.IapDesktop.ProjectExplorer
{
    internal abstract class ProjectExplorerNodeSelectedEvent
    {
    }

    internal class ProjectExplorerCloudNodeSelectedEvent : ProjectExplorerNodeSelectedEvent
    {
    }

    internal class ProjectExplorerProjectNodeSelectedEvent : ProjectExplorerNodeSelectedEvent
    {
        public string ProjectId { get; }

        public ProjectExplorerProjectNodeSelectedEvent(string projectId)
        {
            this.ProjectId = projectId;
        }
    }

    internal class ProjectExplorerZoneNodeSelectedEvent : ProjectExplorerNodeSelectedEvent
    {
        public string ProjectId { get; }
        public string Zone { get; }

        public ProjectExplorerZoneNodeSelectedEvent(string projectId, string zone)
        {
            this.ProjectId = projectId;
            this.Zone = zone;
        }
    }

    internal class ProjectExplorerVmInstanceNodeSelectedEvent : ProjectExplorerNodeSelectedEvent
    {
        public string ProjectId { get; }
        public string Zone { get; }
        public string InstanceName { get; }

        public ProjectExplorerVmInstanceNodeSelectedEvent(string projectId, string zone, string instanceName)
        {
            this.ProjectId = projectId;
            this.Zone = zone;
            this.InstanceName = instanceName;
        }
    }
}
