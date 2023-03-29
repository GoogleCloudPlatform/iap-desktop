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
using Google.Solutions.Mvvm.Binding;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Views.Diagnostics
{
    [ComVisible(false)]
    [SkipCodeCoverage("For debug purposes only")]
    public partial class DebugFullScreenView : DocumentWindow, IView<DebugFullScreenViewModel>
    {
        public DebugFullScreenView(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            InitializeComponent();
        }

        public void Bind(
            DebugFullScreenViewModel viewModel,
            IBindingContext bindingContext)
        {
            this.sizeLabel.BindReadonlyObservableProperty(
                c => c.Text,
                viewModel,
                v => v.SizeLabel,
                bindingContext);

            //
            // NB. Controls are re-parented, thus subscribe to the events
            // of a control, not to those of the form.
            //
            this.groupBox.SizeChanged += (ctl, __) => viewModel.OnWindowSizeChanged(((Control)ctl).FindForm());
            this.groupBox.ParentChanged += (ctl, __) => viewModel.OnWindowSizeChanged(((Control)ctl).FindForm());

            this.tabAccentColorComboBox.BindObservableProperty(
                viewModel.TabAccentColor,
                bindingContext);

            viewModel.TabAccentColor.PropertyChanged += (_, __) =>
            {
                this.DockHandler.TabAccentColor = 
                    (TabAccentColorIndex)viewModel.TabAccentColor.Value;
            };
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void fullScreenToggleButton_Click(object sender, EventArgs e)
        {
            if (IsFullscreen)
            {
                LeaveFullScreen();
            }
            else
            {
                EnterFullscreen(this.allScreensCheckBox.Checked);
            }
        }
    }
}
