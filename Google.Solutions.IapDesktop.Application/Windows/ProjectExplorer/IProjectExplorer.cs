using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.ProjectExplorer
{
    public interface IProjectExplorer
    {
        void ShowWindow();
        Task RefreshProject(string projectId);
        Task RefreshAllProjects();
        Task ShowAddProjectDialogAsync();
    }

}
