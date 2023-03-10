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
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Theme;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Windows
{
    [SkipCodeCoverage("UI code")]
    public partial class PropertiesView : Form, IView<PropertiesViewModel>
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IControlTheme theme;

        private PropertiesViewModel viewModel;
        private IBindingContext bindingContext;

        public Color SheetBackColor { get; set; } = Color.White;

        public PropertiesView(
            IServiceProvider serviceProvider,
            IControlTheme controlTheme)
        {
            this.serviceProvider = serviceProvider.ExpectNotNull(nameof(serviceProvider));
            this.theme = controlTheme;

            InitializeComponent();

            this.Shown += (_, __) => this.tabs.Focus();
        }

        public void Bind(PropertiesViewModel viewModel, IBindingContext bindingContext)
        {
            this.viewModel = viewModel;
            this.bindingContext = bindingContext;

            this.okButton.BindCommand(
                viewModel,
                m => m.OkCommand,
                bindingContext);
            this.applyButton.BindCommand(
                viewModel,
                m => m.ApplyCommand,
                bindingContext);
            this.cancelButton.BindCommand(
                viewModel,
                m => m.CancelCommand,
                bindingContext);
        }

        internal IEnumerable<PropertiesSheetViewModelBase> Sheets => this.viewModel.Sheets;

        internal void AddSheet(
            IPropertiesSheetView view, 
            PropertiesSheetViewModelBase viewModel)
        {
            Debug.Assert(viewModel.View == null, "view model not bound yet");
            
            Debug.Assert(this.viewModel != null);
            Debug.Assert(this.bindingContext != null);

            SuspendLayout();

            //
            // Create control and add it to tabs.
            //
            var viewControl = (ContainerControl)(object)view;
            viewControl.Location = new Point(0, 0);
            viewControl.Dock = DockStyle.Fill;
            viewControl.BackColor = this.SheetBackColor;
            
            var tab = new TabPage()
            {
                BackColor = this.SheetBackColor
            };
            tab.Controls.Add(viewControl);
            this.tabs.TabPages.Add(tab);

            //
            // Bind the sheet and its view model.
            //
            tab.BindReadonlyProperty(
                t => t.Text,
                viewModel,
                m => m.Title,
                this.bindingContext);

            Window<IPropertiesSheetView, PropertiesSheetViewModelBase>.Bind(
                view,
                viewModel,
                this.theme,
                this.bindingContext);

            //
            // Register the (bound) view model.
            //
            this.viewModel.AddSheet(viewModel);

            ResumeLayout();
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public void AddSheet(IPropertiesSheetView view)
        {
            AddSheet(
                view,
                (PropertiesSheetViewModelBase)this.serviceProvider.GetService(view.ViewModel));
        }

        public void AddSheet<TSheet>()
        {
            var view = (IPropertiesSheetView)this.serviceProvider.GetService(typeof(TSheet));
            AddSheet(
                view,
                (PropertiesSheetViewModelBase)this.serviceProvider.GetService(view.ViewModel));
        }
    }
}
