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
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Views.Diagnostics
{
    [ComVisible(false)]
    [SkipCodeCoverage("For debug purposes only")]
    public partial class DebugFullScreenPane : ToolWindow
    {
        private readonly DockPanel dockPanel;

        public DebugFullScreenPane(IServiceProvider serviceProvider)
            : base(serviceProvider, DockState.Document)
        {
            InitializeComponent();
            this.TabText = this.Text;
            this.dockPanel = serviceProvider.GetService<IMainForm>().MainPanel;

            this.DockAreas = DockAreas.Document | DockAreas.Float;
            this.HideOnClose = true;
        }

        protected Rectangle BoundsOfAllMonitors
        {
            get
            {
                Rectangle r = new Rectangle();
                foreach (Screen s in Screen.AllScreens)
                {
                    r = Rectangle.Union(r, s.Bounds);
                }

                return r;
            }
        }

        private Form fullScreenForm = null;
        protected bool IsFullscreen => this.fullScreenForm != null;

        private void MoveControls(Form source, Form target)
        {
            var controls = new Control[source.Controls.Count];
            source.Controls.CopyTo(controls, 0);
            source.Controls.Clear();

            target.Controls.AddRange(controls);
        }

        private void fullScreenToggleButton_Click(object sender, EventArgs e)
        {
            if (this.IsFullscreen)
            {
                MoveControls(this.fullScreenForm, this);
                this.fullScreenForm.Close();
                this.fullScreenForm = null;
            }
            else
            {
                var area = this.BoundsOfAllMonitors;
                this.fullScreenForm = new Form()
                {
                    FormBorderStyle = FormBorderStyle.None,
                    //Location = new Point(area.X, area.Y),
                    //Size = new Size(area.Width, area.Height),
                    Bounds = area,
                    StartPosition = FormStartPosition.Manual,
                    //WindowState = FormWindowState.Maximized,
                    TopMost = true
                };

                MoveControls(this, this.fullScreenForm);
                this.fullScreenForm.Show();
            }
        }
    }
}
