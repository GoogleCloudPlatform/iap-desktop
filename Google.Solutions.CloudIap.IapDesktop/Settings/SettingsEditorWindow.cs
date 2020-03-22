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
    internal partial class SettingsEditorWindow : ToolWindow, ISettingsEditor
    {
        private readonly DockPanel dockPanel;
        private readonly IEventService eventService;
        private readonly InventorySettingsRepository inventorySettingsRepository;

        public SettingsEditorWindow()
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

        public SettingsEditorWindow(DockPanel dockPanel) : this()
        {
            this.dockPanel = dockPanel;
        }

        private void SetEditorObject(object settingsObject)
        {
            // The object might contain a bunch of properties that should not really
            // be displayed, so narrow down the list of properties to properties
            // that have a BrowsableSetting attribute.
            this.propertyGrid.SelectedObject = 
                new FilteringTypeDescriptor<BrowsableSettingAttribute>(settingsObject);
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
                SetEditorObject(e.SelectedNode);
            }
        }

        //---------------------------------------------------------------------
        // ISettingsEditor.
        //---------------------------------------------------------------------

        public void ShowWindow(object settingsObject)
        {
            SetEditorObject(settingsObject);
            Show(this.dockPanel);
            Activate();
        }

        private class FilteringTypeDescriptor<TAttribute> : CustomTypeDescriptor 
            where TAttribute : Attribute, new()
        {
            private readonly object target;

            private static ICustomTypeDescriptor GetTypeDescriptor(object obj)
            {
                var type = obj.GetType();
                var provider = TypeDescriptor.GetProvider(type);
                return provider.GetTypeDescriptor(type, obj);
            }

            public FilteringTypeDescriptor(object target)
                :base(GetTypeDescriptor(target))
            {
                this.target = target;
            }

            public override PropertyDescriptorCollection GetProperties()
            {
                return this.GetProperties(new Attribute[] { });
            }

            public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
            {
                var filteredProperties = base.GetProperties(attributes)
                    .Cast<PropertyDescriptor>()
                    .Where(p => p.Attributes
                        .Cast<Attribute>()
                        .Any(a => a is BrowsableSettingAttribute));

                return new PropertyDescriptorCollection(filteredProperties.ToArray());
            }
        }
    }
}
