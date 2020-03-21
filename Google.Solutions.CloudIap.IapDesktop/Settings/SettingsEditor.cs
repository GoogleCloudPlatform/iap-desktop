using Google.Solutions.CloudIap.IapDesktop.Application.Settings;
using Google.Solutions.CloudIap.IapDesktop.ProjectExplorer;
using Google.Solutions.CloudIap.IapDesktop.Settings;
using Google.Solutions.CloudIap.IapDesktop.Windows;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.CloudIap.IapDesktop.Settings
{
    internal partial class SettingsEditor : ToolWindow, ISettingsEditor
    {
        private readonly DockPanel dockPanel;
        private readonly IEventService eventService;
        private readonly InventorySettingsRepository inventorySettingsRepository;

        public SettingsEditor()
        {
            InitializeComponent();

            this.TabText = this.Text;
            //
            // This window is a singleton, so we never want it to be closed,
            // just hidden.
            //
            this.HideOnClose = true;

            this.eventService = Program.Services.GetService<IEventService>();
            this.inventorySettingsRepository = Program.Services.GetService<InventorySettingsRepository>();

            this.eventService.BindHandler<ProjectExplorerNodeSelectedEvent>(OnProjectExplorerNodeSelected);
        }

        public SettingsEditor(DockPanel dockPanel) : this()
        {
            this.dockPanel = dockPanel;
        }

        private GlobalSettingsEditorNode GetGlobalSettingsEditor()
            => new GlobalSettingsEditorNode(this.inventorySettingsRepository.GetSettings());

        private ProjectSettingsEditorNode GetProjectSettingsEditor(string projectId)
            => new ProjectSettingsEditorNode(
                GetGlobalSettingsEditor(),
                this.inventorySettingsRepository.GetProjectSettings(projectId));

        private ZoneSettingsEditorNode GetZoneSettingsEditor(
            string projectId, 
            string zoneId)
            => new ZoneSettingsEditorNode(
                GetProjectSettingsEditor(projectId),
                this.inventorySettingsRepository.GetZoneSettings(projectId, zoneId));


        private VmInstanceSettingsEditorNode GetVmInstanceSettingsEditor(
            string projectId, 
            string zoneId, 
            string instanceName)
            => new VmInstanceSettingsEditorNode(
                GetZoneSettingsEditor(projectId, zoneId),
                this.inventorySettingsRepository.GetVirtualMachineSettings(projectId, instanceName));

        private void SetEditorNode(SettingsEditorNode settingsNode)
        {
            this.propertyGrid.SelectedObject = settingsNode;
        }

        private SettingsEditorNode LookupEditorNode(IProjectExplorerNode node)
        {
            if (node is IProjectExplorerCloudNode cloudNode)
            {
                return GetGlobalSettingsEditor();
            }
            else if (node is IProjectExplorerProjectNode projectNode)
            {
                return GetProjectSettingsEditor(
                    projectNode.ProjectId);
            }
            else if (node is IProjectExplorerZoneNode zoneNode)
            {
                return GetZoneSettingsEditor(
                    zoneNode.ProjectId,
                    zoneNode.ZoneId);
            }
            else if (node is IProjectExplorerVmInstanceNode vmInstanceNode)
            {
                return GetVmInstanceSettingsEditor(
                    vmInstanceNode.ProjectId,
                    vmInstanceNode.ZoneId,
                    vmInstanceNode.InstanceName);
            }
            else
            {
                throw new ArgumentException("Unrecognized node");
            }
        }

        //---------------------------------------------------------------------
        // Service event handlers.
        //---------------------------------------------------------------------

        private void OnProjectExplorerNodeSelected(ProjectExplorerNodeSelectedEvent e)
        {
            //
            // If the window is visible, switch to a different editor. Otherwise,
            // ignore the event.
            //
            if (this.Visible)
            {
                SetEditorNode(LookupEditorNode(e.SelectedNode));
            }
        }

        //---------------------------------------------------------------------
        // ISettingsEditor.
        //---------------------------------------------------------------------

        public void ShowWindow(SettingsEditorNode settingsNode)
        {
            SetEditorNode(settingsNode);
            Show();
        }

        public void ShowWindow(IProjectExplorerNode node)
        {
            ShowWindow(LookupEditorNode(node));
        }
    }
}
