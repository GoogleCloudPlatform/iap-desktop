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
using Google.Solutions.Mvvm.Binding;
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
        private readonly IExceptionDialog exceptionDialog;

        public PropertiesDialog(IServiceProvider serviceProvider)
        {
            this.exceptionDialog = serviceProvider.GetService<IExceptionDialog>();

            InitializeComponent();
        }

        private DialogResult ApplyChanges()
        {
            try
            {
                foreach (var tab in this.Sheets
                    .Where(t => t.ViewModel.IsDirty))
                {
                    var result = tab.ViewModel.ApplyChanges();

                    if (result == DialogResult.OK)
                    {
                        Debug.Assert(!tab.ViewModel.IsDirty);
                    }
                    else
                    {
                        return result;
                    }
                }

                return DialogResult.OK;
            }
            catch (Exception e)
            {
                this.exceptionDialog.Show(this, "Applying changes failed", e);
                return DialogResult.Cancel;
            }
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public IEnumerable<IPropertiesSheet> Sheets => this.tabs.TabPages
            .Cast<TabPage>()
            .Select(tab => tab.Tag)
            .Cast<IPropertiesSheet>();

        internal void AddSheet(UserControl sheet, IPropertiesSheet sheetInterface)
        {
            Debug.Assert(sheet == sheetInterface);

            //
            // Create control and add it to tabs.
            //
            var tab = new TabPage();
            sheet.Location = new Point(0, 0);
            sheet.Dock = DockStyle.Fill;
            sheet.BackColor = Color.White;
            tab.Controls.Add(sheet);
            this.tabs.TabPages.Add(tab);

            tab.BindReadonlyProperty(
                t => t.Text,
                sheetInterface.ViewModel,
                m => m.Title,
                this.Container);
            sheetInterface.ViewModel.OnPropertyChange(
                m => m.IsDirty,
                _ =>
                {
                    //
                    // Enable the Apply button if any of the panes goes dirty.
                    //
                    this.applyButton.Enabled = this.Sheets.Any(p => p.ViewModel.IsDirty);
                });

            //
            // Set tag so that we can access the object later.
            //
            tab.Tag = sheet;
        }

        internal void AddSheet<TSheet>(TSheet sheet)
            where TSheet : UserControl, IPropertiesSheet
            => AddSheet(sheet, sheet);

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void okButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = ApplyChanges();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void applyButton_Click(object sender, EventArgs e)
        {
            ApplyChanges();
        }
    }
}
