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

using System;
using System.Drawing;
using System.Windows.Forms;
using Google.Solutions.IapDesktop.Application.Properties;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application.Views;

namespace Google.Solutions.IapDesktop.Application.Controls
{
    [SkipCodeCoverage("Pure UI code")]
    public partial class SearchableList<TModelItem> : UserControl
    {
        public event EventHandler LoadingChanged;
        public event EventHandler SearchTermChanged;

        public string SearchTerm
        {
            get => this.searchTextBox.Text;
            set => this.searchTextBox.Text = value;
        }

        public SearchableList()
        {
            InitializeComponent();
        }

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
            this.list.Columns.Add(text, width);
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void ProjectList_Load(object sender, EventArgs e)
        {
            //
            // Add Search button as overlay.
            //
            var searchButton = new Button();
            searchButton.Size = new Size(16, 16);//, this.searchTextBox.ClientSize.Height + 2);
            searchButton.Location = new Point(this.searchTextBox.ClientSize.Width - searchButton.Width - 4, 2);
            searchButton.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            searchButton.FlatStyle = FlatStyle.Flat;
            searchButton.FlatAppearance.BorderSize = 0;
            searchButton.FlatAppearance.MouseOverBackColor = searchButton.BackColor;
            searchButton.BackColorChanged += (s, _) => {
                searchButton.FlatAppearance.MouseOverBackColor = searchButton.BackColor;
            };
            searchButton.TabStop = false;
            searchButton.Image = Resources.Search_16;
            searchButton.Cursor = Cursors.Default;
            searchButton.Click += (s, a) => StartSearch();
            this.searchTextBox.Controls.Add(searchButton);

            // Send EM_SETMARGINS to prevent text from disappearing underneath the button
            UnsafeNativeMethods.SendMessage(
                this.searchTextBox.Handle,
                UnsafeNativeMethods.EM_SETMARGINS,
                (IntPtr)2,
                (IntPtr)(searchButton.Width << 16));
        }

        private void searchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                StartSearch();
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
