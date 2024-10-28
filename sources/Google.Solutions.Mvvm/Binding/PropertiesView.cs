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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Mvvm.Theme;
using System.Drawing;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Binding
{
    [SkipCodeCoverage("UI code")]
    public partial class PropertiesView 
        : CompositeForm, IView<PropertiesViewModel>, IThemedView
    {
        private IControlTheme? theme;

        public PropertiesView()
        {
            InitializeComponent();

            this.Shown += (_, __) => this.tabs.Focus();
        }

        public void SetTheme(IControlTheme theme)
        {
            this.theme = theme.ExpectNotNull(nameof(theme));
        }

        public void Bind(PropertiesViewModel viewModel, IBindingContext bindingContext)
        {
            this.BindReadonlyObservableProperty(
                c => c.Text,
                viewModel,
                m => m.WindowTitle,
                bindingContext);

            //
            // Bind commands.
            //
            this.okButton.BindObservableCommand(
                viewModel,
                m => m.OkCommand,
                bindingContext);
            this.applyButton.BindObservableCommand(
                viewModel,
                m => m.ApplyCommand,
                bindingContext);
            this.cancelButton.BindObservableCommand(
                viewModel,
                m => m.CancelCommand,
                bindingContext);

            //
            // Bind sheets.
            //
            foreach (var sheet in viewModel.Sheets)
            {
                //
                // Create control and add it to tabs.
                //
                var viewControl = (ContainerControl)(object)sheet.View;
                viewControl.Location = new Point(0, 0);
                viewControl.Dock = DockStyle.Fill;
                viewControl.BackColor = this.tabs.SheetBackColor;

                var tab = new TabPage()
                {
                    BackColor = this.tabs.SheetBackColor
                };
                tab.Controls.Add(viewControl);
                this.tabs.TabPages.Add(tab);

                //
                // Bind the sheet and its view model.
                //
                tab.BindReadonlyProperty(
                    t => t.Text,
                    sheet.ViewModel,
                    m => m.Title,
                    bindingContext);

                TopLevelWindow<IPropertiesSheetView, PropertiesSheetViewModelBase, IControlTheme>.Bind(
                    sheet.View,
                    sheet.ViewModel,
                    this.theme,
                    bindingContext);
            }
        }
    }
}
