namespace Google.Solutions.IapDesktop.Application.ProjectExplorer
{
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
