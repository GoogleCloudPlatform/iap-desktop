using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.ProjectExplorer;
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
using Google.Solutions.IapDesktop.Application.Windows;

namespace Google.Solutions.IapDesktop.Application.SettingsEditor
{
    public partial class SettingsEditorWindow : ToolWindow, ISettingsEditor
    {
        private readonly DockPanel dockPanel;
        private readonly IEventService eventService;
        private readonly InventorySettingsRepository inventorySettingsRepository;

        public SettingsEditorWindow()
        {
        }

        public SettingsEditorWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            this.dockPanel = serviceProvider.GetService<IMainForm>().MainPanel;

            this.TabText = this.Text;
            //
            // This window is a singleton, so we never want it to be closed,
            // just hidden.
            //
            this.HideOnClose = true;

            this.eventService = serviceProvider.GetService<IEventService>();
            this.inventorySettingsRepository = serviceProvider.GetService<InventorySettingsRepository>();

            this.eventService.BindHandler<ProjectExplorerNodeSelectedEvent>(OnProjectExplorerNodeSelected);
        }

        private ISettingsObject EditorObject
        {
            set
            {
                if (value == null)
                {
                    this.propertyGrid.SelectedObject = null;
                }
                else
                {
                    // The object might contain a bunch of properties that should not really
                    // be displayed, so narrow down the list of properties to properties
                    // that have a BrowsableSetting attribute.
                    this.propertyGrid.SelectedObject =
                        new FilteringTypeDescriptor<ISettingsObject, BrowsableSettingAttribute>(value);
                }
            }
            get
            {
                return ((FilteringTypeDescriptor<ISettingsObject, BrowsableSettingAttribute>)
                    this.propertyGrid.SelectedObject).Target;
            }
        }

        //---------------------------------------------------------------------
        // Window event handlers.
        //---------------------------------------------------------------------

        private void propertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            this.EditorObject.SaveChanges();
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
            if (this.Visible && e.SelectedNode is ISettingsObject settingsObject)
            {
                this.EditorObject = settingsObject;
            }
            else
            {
                this.EditorObject = null;
            }
        }

        //---------------------------------------------------------------------
        // ISettingsEditor.
        //---------------------------------------------------------------------

        public void ShowWindow(ISettingsObject settingsObject)
        {
            this.EditorObject = settingsObject;
            Show(this.dockPanel);
            Activate();
        }


        //---------------------------------------------------------------------
        // Custom type descriptor.
        //---------------------------------------------------------------------

        private class FilteringTypeDescriptor<T, TAttribute> : CustomTypeDescriptor 
            where TAttribute : Attribute, new()
        {
            public T Target { get; }

            private static ICustomTypeDescriptor GetTypeDescriptor(object obj)
            {
                var type = obj.GetType();
                var provider = TypeDescriptor.GetProvider(type);
                return provider.GetTypeDescriptor(type, obj);
            }

            public FilteringTypeDescriptor(T target)
                :base(GetTypeDescriptor(target))
            {
                this.Target = target;
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
