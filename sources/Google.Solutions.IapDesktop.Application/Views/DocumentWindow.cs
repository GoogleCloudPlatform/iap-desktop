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
using Google.Solutions.IapDesktop.Application.Properties;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Views
{
    public class DocumentWindow : ToolWindow
    {
        private Form fullScreenForm = null;

        private static void MoveControls(Form source, Form target)
        {
            var controls = new Control[source.Controls.Count];
            source.Controls.CopyTo(controls, 0);
            source.Controls.Clear();
            target.Controls.AddRange(controls);

            Debug.Assert(source.Controls.Count == 0);
        }

        protected static Rectangle BoundsOfAllScreens
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

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public DocumentWindow()
        {
            // Constructor is for designer only.
        }

        public DocumentWindow(
            IServiceProvider serviceProvider) 
            : base(serviceProvider, DockState.Document)
        {
            this.DockAreas = DockAreas.Document;
        }

        //---------------------------------------------------------------------
        // Full-screen support.
        //---------------------------------------------------------------------

        protected bool IsFullscreen => this.fullScreenForm != null;

        protected void EnterFullscreen(bool allScreens)
        {
            using (ApplicationTraceSources.Default.TraceMethod()
                .WithParameters(allScreens))
            {
                Debug.Assert(this.fullScreenForm == null);
                if (this.fullScreenForm != null)
                {
                    // In full screen mode already.
                    return;
                }

                var bounds = allScreens
                    ? BoundsOfAllScreens
                    : Screen.PrimaryScreen.Bounds;

                //
                // NB. You can make a docking window full-screen, but it
                // is not possible to hide their frame. To provide a true
                // full screen experience, create a new window and 
                // temporarily move all controls to this window.
                //

                this.fullScreenForm = new Form()
                {
                    Icon = Resources.logo,
                    FormBorderStyle = FormBorderStyle.None,
                    Bounds = bounds,
                    StartPosition = FormStartPosition.Manual,
                    TopMost = true,
                    WindowState = FormWindowState.Maximized
                };

                MoveControls(this, this.fullScreenForm);
                this.fullScreenForm.Show();
            }
        }

        protected void LeaveFullScreen()
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            {
                Debug.Assert(this.fullScreenForm != null);
                if (this.fullScreenForm == null)
                {
                    // Not in full screen mode already.
                    return;
                }

                MoveControls(this.fullScreenForm, this);
                this.fullScreenForm.Close();
                this.fullScreenForm = null;
            }
        }
    }
}
