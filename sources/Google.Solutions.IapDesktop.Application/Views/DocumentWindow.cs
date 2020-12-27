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

using System;
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
        }

        private static Rectangle BoundsOfAllScreens
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

        public DocumentWindow(
            IServiceProvider serviceProvider,
            DockState defaultDockState) 
            : base(serviceProvider, defaultDockState)
        {
        }

        //---------------------------------------------------------------------
        // Multi-monitor full-screen.
        //---------------------------------------------------------------------

        public bool MultiScreenFullScreen
        {
            get => this.fullScreenForm != null;
            set
            {
                if (this.fullScreenForm != null)
                {
                    MoveControls(this.fullScreenForm, this);
                    this.fullScreenForm.Close();
                    this.fullScreenForm = null;
                }
                else
                {
                    var area = BoundsOfAllScreens;
                    this.fullScreenForm = new Form()
                    {
                        // TODO: set icon
                        // TODO: Handle Win+Down

                        FormBorderStyle = FormBorderStyle.None,
                        Bounds = area,
                        StartPosition = FormStartPosition.Manual,
                        TopMost = true
                    };

                    MoveControls(this, this.fullScreenForm);
                    this.fullScreenForm.Show();
                }
            }
        }
    }
}
