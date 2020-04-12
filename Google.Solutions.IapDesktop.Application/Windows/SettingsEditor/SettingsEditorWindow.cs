//
// Copyright 2020 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.Windows;
using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.SettingsEditor
{
    public partial class SettingsEditorWindow : ToolWindow, ISettingsEditor
    {
        private const int GoldBarHeight = 22;

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
            this.ShowGoldBar = false;

            //
            // This window is a singleton, so we never want it to be closed,
            // just hidden.
            //
            this.HideOnClose = true;

            this.eventService = serviceProvider.GetService<IEventService>();
            this.inventorySettingsRepository = serviceProvider.GetService<InventorySettingsRepository>();

            this.eventService.BindHandler<ProjectExplorerNodeSelectedEvent>(OnProjectExplorerNodeSelected);
        }

        public bool ShowGoldBar
        {
            get
            {
                return this.goldBar.Height > 0;
            }
            set
            {
                if (value == this.goldBar.Height > 0)
                {
                    // No change.
                    return;
                }

                if (value)
                {
                    this.goldBar.Height = GoldBarHeight;
                    this.propertyGrid.Location = new Point(
                        this.propertyGrid.Location.X,
                        this.propertyGrid.Location.Y + GoldBarHeight);
                    this.propertyGrid.Size = new Size(
                        this.propertyGrid.Size.Width,
                        this.propertyGrid.Size.Height - GoldBarHeight);
                }
                else
                {
                    this.goldBar.Height = 0;
                    this.propertyGrid.Location = new Point(
                        this.propertyGrid.Location.X,
                        this.propertyGrid.Location.Y - GoldBarHeight);
                    this.propertyGrid.Size = new Size(
                        this.propertyGrid.Size.Width,
                        this.propertyGrid.Size.Height + GoldBarHeight);
                }
            }
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

                    // Update gold bar.
                    this.ShowGoldBar = value.InformationText != null;
                    this.infoLabel.Text = value.InformationText ?? string.Empty;
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
        private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            var property = this.propertyGrid.SelectedGridItem?.PropertyDescriptor;

            // We only allow resetting strings.
            this.resetToolStripMenuItem.Enabled =
                property != null && property.PropertyType == typeof(string);
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var property = this.propertyGrid.SelectedGridItem?.PropertyDescriptor;

            if (property != null)
            {
                property.SetValue(this.propertyGrid.SelectedObject, null);

                // The grid does not notice this change, so we need to explicitly
                // save and refresh.

                this.EditorObject.SaveChanges();
                this.propertyGrid.Refresh();
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
            ShowOrActivate(this.dockPanel, DockState.DockRightAutoHide);
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
                : base(GetTypeDescriptor(target))
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
