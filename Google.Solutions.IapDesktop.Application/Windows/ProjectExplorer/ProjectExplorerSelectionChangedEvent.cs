namespace Google.Solutions.IapDesktop.Application.ProjectExplorer
{
    internal class ProjectExplorerNodeSelectedEvent
    {
        public IProjectExplorerNode SelectedNode { get; }

        public ProjectExplorerNodeSelectedEvent(IProjectExplorerNode selectedNode)
        {
            this.SelectedNode = selectedNode;
        }
    }
}
