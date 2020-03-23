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
