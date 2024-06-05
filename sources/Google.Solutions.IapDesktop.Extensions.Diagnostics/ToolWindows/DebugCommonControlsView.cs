//
// Copyright 2023 Google LLC
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

using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Diagnostics.ToolWindows
{
    [Service]
    public partial class DebugCommonControlsView : ToolWindowViewBase, IView<DebugCommonControlsViewModel>
    {
        public DebugCommonControlsView(
            IMainWindow mainWindow,
            ToolWindowStateRepository stateRepository)
            : base(
                  mainWindow,
                  stateRepository,
                  WeifenLuo.WinFormsUI.Docking.DockState.DockLeft)
        {
            InitializeComponent();

            for (var i = 0; i < 10; i++)
            {
                var item = new ListViewItem()
                {
                    Text = $"Name {i}"
                };
                item.SubItems.AddRange(new[] { $"Value {i}" });
                this.listView.Items.Add(item);
            }
        }

        public void Bind(
            DebugCommonControlsViewModel viewModel,
            IBindingContext bindingContext)
        {
            this.richTextBox.Rtf =
                @"{\rtf1\ansi{\fonttbl\f0\fswiss Helvetica;}\f0\pard
                 This is some {\b bold} text.\par
                 This is some {\b bold} text.\par
                 This is some {\b bold} text.\par
                 This is some {\b bold} text.\par
                 This is some {\b bold} text.\par
                 This is some {\b bold} text.\par
                 }";

            //
            // Enable
            //
            this.textBoxEnabled.BindObservableProperty(
                c => c.Checked,
                viewModel,
                m => m.ControlEnabled,
                bindingContext);

            foreach (var control in new Control[]
            {
                this.regularButton,
                this.okButton,
                this.cancelButton,
                this.label,
                this.linkLabel,
                this.checkBox,
                this.radioButton,
                this.textBox,
                this.multilineTextBox,
                this.richTextBox,
                this.comboBox,
                this.numericUpDown,
                this.listView,
                this.dropDownButton
            })
            {
                control.BindReadonlyObservableProperty(
                    c => c.Enabled,
                    viewModel,
                    m => m.ControlEnabled,
                    bindingContext);
            }
            foreach (var item in new ToolStripItem[]
            {
                this.toolStripButton,
                this.toolStripDropDownButton,
                this.toolStripComboBox
            })
            {
                item.BindReadonlyObservableProperty(
                    c => c.Enabled,
                    viewModel,
                    m => m.ControlEnabled,
                    bindingContext);
            }

            //
            // Readonly.
            //
            this.readOnlyCheckBox.BindObservableProperty(
                c => c.Checked,
                viewModel,
                m => m.ControlReadonly,
                bindingContext);

            foreach (var textBoxIsh in new TextBoxBase[]
            {
                this.textBox,
                this.multilineTextBox,
                this.richTextBox,
            })
            {
                textBoxIsh.BindReadonlyObservableProperty(
                    c => c.ReadOnly,
                    viewModel,
                    m => m.ControlReadonly,
                    bindingContext);

            }
        }
    }
}
