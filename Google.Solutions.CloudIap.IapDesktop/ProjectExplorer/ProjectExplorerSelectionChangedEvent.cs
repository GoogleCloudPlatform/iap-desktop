using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.CloudIap.IapDesktop.ProjectExplorer
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
