using Google.Solutions.CloudIap.IapDesktop.ProjectExplorer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.CloudIap.IapDesktop.Settings
{
    internal interface ISettingsEditor
    {
        void ShowWindow(SettingsEditorNode settingsNode);
        void ShowWindow(IProjectExplorerNode projectExplorerNode);
    }
}
