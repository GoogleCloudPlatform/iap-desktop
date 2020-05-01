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

using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.ProjectExplorer
{
    public interface IProjectPickerDialog
    {
        string SelectProjectId(IWin32Window owner);
    }

    public partial class ProjectPickerDialog : Form, IProjectPickerDialog
    {
        private const int MinimumInputLengthForAutocomplete = 2;
        private bool clearedInput = false;
        private bool updatingSuggestions = false;
        private string suggestionsPrefix = null;
        private readonly IResourceManagerAdapter resourceManager;

        public ProjectPickerDialog(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            this.resourceManager = serviceProvider.GetService<IResourceManagerAdapter>();
        }

        private void projectComboBox_Enter(object sender, EventArgs e)
        {
            if (!this.clearedInput)
            {
                this.projectComboBox.Text = string.Empty;
                this.clearedInput = true;
            }
        }

        private async void projectComboBox_TextUpdate(object sender, EventArgs e)
        {
            bool minLengthEntered = this.projectComboBox.Text.Length >= MinimumInputLengthForAutocomplete;

            this.okButton.Enabled = minLengthEntered;

            // Use a flag to protected against reentrant calls.
            if (minLengthEntered && !updatingSuggestions)
            {
                this.updatingSuggestions = true;
                await UpdateSuggestionsAsync(this.projectComboBox.Text);

                this.updatingSuggestions = false;
            }
        }

        private async Task UpdateSuggestionsAsync(string prefix)
        {
            if (this.suggestionsPrefix != null && prefix.StartsWith(this.suggestionsPrefix))
            {
                // Current list items are a superset of what is needed, that is ok.
            }
            else
            {
                // Current list items do not match the prefix - clear suggestions and reload.
                var suggestions = new AutoCompleteStringCollection();

                foreach (var project in await this.resourceManager.QueryProjectsByPrefix(prefix))
                {
                    suggestions.Add($"{project.Name} ({project.ProjectId})");
                }

                this.projectComboBox.AutoCompleteCustomSource = suggestions;
                this.suggestionsPrefix = prefix;

                // Updating the autocompleter causes all text to become selected.
                this.projectComboBox.SelectionStart = this.projectComboBox.Text.Length;
                this.projectComboBox.SelectionLength = 0;
            }
        }

        private string SelectedProjectId
        {
            get
            {
                // The auto completer does not support associating any kind of tag,
                // so we have to extract the project ID from the displayed text.
                // If the entered text does not match the pattern produced by the
                // automcompleter, then it is probably a project ID that the user
                // pasted (or entered faster than the autocomplee
                var match = new Regex(@".*\((.+)\)").Match(this.projectComboBox.Text);
                return match.Success ? match.Groups[1].Value : this.projectComboBox.Text;
            }
        }

        private async void okButton_Click(object sender, EventArgs e)
        {
            // NB. The DialogResult property of the button is not set so that this
            // event handler is run to completion *before* ShowDialog returns.
            var project = await this.resourceManager.QueryProjectsById(this.SelectedProjectId);
            if (project.Any())
            {
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                // Invalid project ID.
                MessageBox.Show(
                    this,
                    "The project does not exist or you do not have the permission to access it",
                    this.Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                this.DialogResult = DialogResult.None;
            }
        }

        public string SelectProjectId(IWin32Window owner)
        {
            if (ShowDialog(owner) == DialogResult.OK)
            {
                return this.SelectedProjectId;
            }
            else
            {
                return null;
            }
        }
    }
}
