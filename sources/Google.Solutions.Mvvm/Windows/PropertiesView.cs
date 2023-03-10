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
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Windows
{
    [SkipCodeCoverage("UI code")]
    public partial class PropertiesView : Form, IView<PropertiesViewModel>
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IControlTheme theme;

        public Color SheetBackColor { get; set; } = Color.White;

        protected PropertiesView(
            IServiceProvider serviceProvider,
            IControlTheme controlTheme)
        {
            this.serviceProvider = serviceProvider.ExpectNotNull(nameof(serviceProvider));
            this.theme = controlTheme;

            SuspendLayout();

            InitializeComponent();
            ResumeLayout();

            this.Shown += (_, __) => this.tabs.Focus();
        }

        public void Bind(PropertiesViewModel viewModel, IBindingContext bindingContext)
        {
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

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public IEnumerable<IPropertiesSheetView> Sheets => this.tabs.TabPages
            .Cast<TabPage>()
            .Select(tab => tab.Tag)
            .Cast<IPropertiesSheetView>();

        private void AddSheet(
            IPropertiesSheetView view, 
            PropertiesSheetViewModelBase viewModel,
            IBindingContext bindingContext)
        {
            Debug.Assert(viewModel.View == null, "view model not bound yet");

            SuspendLayout();

            //
            // Create control and add it to tabs.
            //
            var viewControl = (ContainerControl)(object)view;
            viewControl.Location = new Point(0, 0);
            viewControl.Dock = DockStyle.Fill;
            viewControl.BackColor = this.SheetBackColor;
            this.theme.ApplyTo(viewControl);

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
                bindingContext);

            Window<IPropertiesSheetView, PropertiesSheetViewModelBase>.Bind(
                view,
                viewModel,
                this.theme,
                bindingContext);

            //
            // Set tag so that we can access the object later.
            //
            tab.Tag = view;

            ResumeLayout();
        }

        public void AddSheet<TSheet>(
            IPropertiesSheetView view,
            IBindingContext bindingContext)
            where TSheet : IPropertiesSheetView
        {
            AddSheet(
                view,
                (PropertiesSheetViewModelBase)this.serviceProvider.GetService(view.ViewModel),
                bindingContext);
        }

        public void AddSheet<TSheet>(
            IBindingContext bindingContext)
            where TSheet : IPropertiesSheetView
        {
            var view = (IPropertiesSheetView)this.serviceProvider.GetService(typeof(TSheet));
            AddSheet(
                view,
                (PropertiesSheetViewModelBase)this.serviceProvider.GetService(view.ViewModel),
                bindingContext);
        }
    }
}
