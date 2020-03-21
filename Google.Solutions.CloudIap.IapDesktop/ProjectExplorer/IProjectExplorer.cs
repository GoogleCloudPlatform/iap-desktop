using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.CloudIap.IapDesktop.ProjectExplorer
{
    internal interface IProjectExplorer
    {
        void ShowWindow();
        Task RefreshProject(string projectId);
        Task RefreshAllProjects();
    }

    internal interface IProjectExplorerNode
    {
    }

    internal interface IProjectExplorerCloudNode : IProjectExplorerNode
    {
    }

    internal interface IProjectExplorerProjectNode : IProjectExplorerNode
    {
        string ProjectId { get; }
    }

    internal interface IProjectExplorerZoneNode : IProjectExplorerNode
    {
        string ProjectId { get; }
        string ZoneId { get; }
    }

    internal interface IProjectExplorerVmInstanceNode : IProjectExplorerNode
    {
        string ProjectId { get; }
        string ZoneId { get; }
        string InstanceName { get; }
    }
}
