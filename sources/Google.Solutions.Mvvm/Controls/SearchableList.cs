//
// Copyright 2019 Google LLC
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
using Google.Solutions.Mvvm.Properties;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

#pragma warning disable IDE1006 // Naming Styles

namespace Google.Solutions.Mvvm.Controls
{
    [SkipCodeCoverage("Pure UI code")]
    public partial class SearchableList<TModelItem> : DpiAwareUserControl
    {
        public event EventHandler? LoadingChanged;
        public event EventHandler? SearchTermChanged;

        public SearchableList()
        {
            InitializeComponent();
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        [Browsable(true)]
        public bool SearchOnKeyDown { get; set; } = false;

        [Browsable(true)]
        public bool MultiSelect
        {
            get => this.list.MultiSelect;
            set => this.list.MultiSelect = value;
        }

        [Browsable(true)]
        public string SearchTerm
        {
            get => this.searchTextBox.Text;
            set => this.searchTextBox.Text = value;
        }

        [Browsable(false)]
        public bool Loading
        {
            get => this.progressBar.Enabled;
            set
            {
                this.progressBar.Enabled = value;
                this.progressBar.Visible = value;
                this.LoadingChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public BindableListView<TModelItem> List => this.list;

        public void AddColumn(string text, int width)
        {
            this.list.Columns.Add(text, LogicalToDeviceUnits(width));
        }

        public void SetFocusOnSearchBox()
        {
            this.searchTextBox.Focus();
            this.searchTextBox.SelectAll();
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void ProjectList_Load(object sender, EventArgs e)
        {
            //
            // Add Search button as overlay.
            //
            var searchButton = this.searchTextBox.AddOverlayButton(Resources.Search_16);
            searchButton.Click += (s, a) => StartSearch();
        }

        private void searchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                this.list.Focus();
            }
            else if (this.SearchOnKeyDown || e.KeyCode == Keys.Enter)
            {
                StartSearch();
            }
        }

        private void List_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A && e.Control && this.list.MultiSelect)
            {
                foreach (ListViewItem item in this.list.Items)
                {
                    item.Selected = true;
                }
            }
        }

        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            base.ScaleControl(factor, specified);

            //
            // NB. Scaling can produce a gap between the controls. Rearrange controls
            // to remove this gap.
            //
            var gap = this.progressBar.Location.Y - 
                this.searchTextBox.Location.Y - 
                this.searchTextBox.Height;

            if (gap > 0)
            {
                this.progressBar.Top -= gap;
                this.list.Top -= gap;
                this.list.Height += gap;
            }
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        private void StartSearch()
        {
            this.SearchTermChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
