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
using System.Linq;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Views
{
    public class DocumentWindow : ToolWindow
    {
        //
        // Full screen form -- created lazily. There can only be one window
        // full scnreen at a time, so it's static.
        //
        private static Form fullScreenForm = null;

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

        protected static bool IsFullscreen 
            => fullScreenForm != null && fullScreenForm.Visible;

        protected void EnterFullscreen(bool allScreens)
        {
            using (ApplicationTraceSources.Default.TraceMethod()
                .WithParameters(allScreens))
            {
                if (IsFullscreen)
                {
                    // In full screen mode already.
                    return;
                }

                //
                // NB. You can make a docking window full-screen, but it
                // is not possible to hide its frame. To provide a true
                // full screen experience, we create a new window and 
                // temporarily move all controls to this window.
                //
                // NB. The RDP ActiveX has some quirk where the connection bar
                // disappears when you go full-screen a second time and the
                // hosting window is different from the first time.
                // By using a single/static window and keeping it around
                // after first use, we ensure that the form is always the
                // same, thus circumventing the quirk.
                //

                if (fullScreenForm == null)
                {
                    //
                    // First time to go full screen, create the
                    // full-screen window.
                    //
                    fullScreenForm = new Form()
                    {
                        Icon = Resources.logo,
                        FormBorderStyle = FormBorderStyle.None,
                        StartPosition = FormStartPosition.Manual,
                        TopMost = true,
                    };
                }

                fullScreenForm.Bounds = allScreens
                    ? BoundsOfAllScreens
                    : Screen.FromControl(this).Bounds;

                MoveControls(this, fullScreenForm);
                fullScreenForm.Show();
            }
        }

        protected void LeaveFullScreen()
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            {
                Debug.Assert(IsFullscreen);
                if (!IsFullscreen)
                {
                    // Not in full screen mode.
                    return;
                }

                MoveControls(fullScreenForm, this);

                //
                // Only hide the window, we might need it again.
                //
                fullScreenForm.Hide();
            }
        }
    }
}
