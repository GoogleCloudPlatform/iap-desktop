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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.Properties
{
    [SkipCodeCoverage("UI code")]
    public partial class PropertiesDialog : Form
    {
        private readonly IServiceProvider serviceProvider;

        public PropertiesDialog(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            InitializeComponent();
        }

        private bool ApplyChanges()
        {
            try
            {
                foreach (var tab in this.Panes
                    .Where(t => t.IsDirty))
                {
                    tab.ApplyChanges();
                    Debug.Assert(!tab.IsDirty);
                }

                return true;
            }
            catch (Exception e)
            {
                this.serviceProvider.GetService<IExceptionDialog>()
                    .Show(this, "Applying changes failed", e);
                return false;
            }
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public IEnumerable<IPropertiesDialogPane> Panes => this.tabs.TabPages
            .Cast<TabPage>()
            .Select(tab => tab.Tag)
            .Cast<IPropertiesDialogPane>();

        public void AddPane(IPropertiesDialogPane pane)
        {
            // Create control and add it to tabs.
            var tab = new TabPage();
            var control = pane.CreateControl();
            control.Location = new Point(0, 0);
            control.Dock = DockStyle.Fill;
            control.BackColor = Color.White;
            tab.Controls.Add(control);
            this.tabs.TabPages.Add(tab);

            tab.BindProperty(
                t => t.Text,
                pane,
                m => m.Title,
                this.Container);
            pane.OnPropertyChange(
                m => m.IsDirty,
                _ =>
                {
                    // Enable the Apply button if any of the panes goes dirty.
                    this.applyButton.Enabled = this.Panes.Any(p => p.IsDirty);
                });

            // Set tag so that we can access the object later.
            tab.Tag = pane;
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void okButton_Click(object sender, EventArgs e)
        {
            if (ApplyChanges())
            {
                this.DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

        private void applyButton_Click(object sender, EventArgs e)
        {
            ApplyChanges();
        }
    }
}
