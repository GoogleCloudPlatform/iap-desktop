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
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Properties;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Util;
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
        /// <summary>
        /// Hotkey to move focus to current document, or release focus
        /// back to main window.
        /// </summary>
        public const Keys ToggleFocusHotKey = Keys.Control | Keys.Alt | Keys.Home;

        /// <summary>
        /// Hotkey to enter full-screen.
        /// </summary>
        public const Keys EnterFullScreenHotKey = Keys.F11;

        /// <summary>
        /// Hotkey to leave full-screen.
        /// </summary>
        public const Keys LeaveFullScreenHotKey = Keys.Control | Keys.Alt | Keys.F11;

        //
        // Full screen form -- created lazily. There can only be one window
        // full scnreen at a time, so it's static.
        //
        private static Form fullScreenForm = null;

        protected IMainForm MainForm { get; }
        private readonly ApplicationSettingsRepository settingsRepository;

        protected static void MoveControls(Form source, Form target)
        {
            var controls = new Control[source.Controls.Count];
            source.Controls.CopyTo(controls, 0);
            source.Controls.Clear();
            target.Controls.AddRange(controls);

            Debug.Assert(source.Controls.Count == 0);
        }

        protected Rectangle BoundsOfAllScreens
        {
            get
            {
                //
                // Read list of screen devices to use.
                // 
                // NB. The list of devices might include devices that
                // do not exist anymore. 
                //
                var selectedDevices = (this.settingsRepository.GetSettings()
                    .FullScreenDevices.StringValue ?? string.Empty)
                        .Split(ApplicationSettings.FullScreenDevicesSeparator)
                        .ToHashSet();

                var screens = Screen.AllScreens
                    .Where(s => selectedDevices.Contains(s.DeviceName));

                if (!screens.Any())
                {
                    // Default to all screens.
                    screens = Screen.AllScreens;
                }

                Rectangle r = new Rectangle();
                foreach (var s in screens)
                {
                    r = Rectangle.Union(r, s.Bounds);
                }

                return r;
            }
        }

        /// <summary>
        /// Size of window when it was not floating.
        /// </summary>
        protected Size? PreviousNonFloatingSize { get; private set; }

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
            this.settingsRepository = serviceProvider.GetService<ApplicationSettingsRepository>();
            this.MainForm = serviceProvider.GetService<IMainForm>();

            this.DockAreas = DockAreas.Document | DockAreas.Float;

            this.SizeChanged += (sender, args) =>
            {
                if (this.Pane?.FloatWindow == null)
                {
                    //
                    // Keep track of size for as long as the window
                    // isn't floating.
                    //
                    this.PreviousNonFloatingSize = this.Size;
                }
                else if (this.PreviousNonFloatingSize != null &&
                    this.Size != this.PreviousNonFloatingSize.Value &&
                    Math.Abs(this.Size.Width - this.PreviousNonFloatingSize.Value.Width) <= 2 &&
                    Math.Abs(this.Size.Height - this.PreviousNonFloatingSize.Value.Height) <= 2)
                {
                    //
                    // The floating size is really close to the size the window had when
                    // it was non-floating. This discrepancy is most likely caused by the
                    // docking library and is unintentional (we try to size the window
                    // so that it fits the required client size, but that doesn't always
                    // match exactly).
                    //
                    // Adjust the size so that it fits the previous size. That way,
                    // we avoid having to resize the contents (which can be expensive).
                    //
                    this.Size = this.PreviousNonFloatingSize.Value;
                }
            };
        }

        //---------------------------------------------------------------------
        // Full-screen support.
        //---------------------------------------------------------------------

        protected static bool IsFullscreen
            => fullScreenForm != null && fullScreenForm.Visible;

        protected static bool IsFullscreenMinimized
            => fullScreenForm != null && fullScreenForm.WindowState == FormWindowState.Minimized;

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
                        ShowInTaskbar = false
                    };
                }

                fullScreenForm.Bounds = allScreens
                    ? BoundsOfAllScreens
                    : Screen.FromControl(this).Bounds;

                MoveControls(this, fullScreenForm);

                //
                // Make parent of main form so that when we minimize/
                // restore, this window comes up front.
                //
                fullScreenForm.Show(this.MainForm.Window);
            }
        }

        protected void LeaveFullScreen()
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            {
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

        protected void MinimizeWindow()
        {
            if (!IsFullscreen)
            {
                // Not in full screen mode.
                return;
            }

            //
            // Minimize this window.
            //
            fullScreenForm.WindowState = FormWindowState.Minimized;

            //
            // Minimize the main form (which is still running in the 
            // back)
            //
            this.MainForm.Minimize();
        }

        protected override void WndProc(ref Message m)
        {
            if (!this.DesignMode)
            {
                switch (m.Id())
                {
                    case WindowMessage.WM_SHOWWINDOW:
                        if (this.Pane?.FloatWindow != null)
                        {
                            //
                            // Window has been torn off and is now entering a floating state.
                            // 
                            // Set the size of the floating window so that it fits the 
                            // required client size.
                            //
                            var nonClientOverhead = new Size
                            {
                                Width = this.Pane.FloatWindow.Width - this.Pane.FloatWindow.ClientRectangle.Width,
                                Height = this.Pane.FloatWindow.Height - this.Pane.FloatWindow.ClientRectangle.Height
                            };

                            var targetSize = this.DefaultFloatWindowClientSize + nonClientOverhead;
                            if (this.Pane.FloatWindow.Size != targetSize)
                            {
                                this.Pane.FloatWindow.Size = targetSize;
                            }
                        }
                        break;
                }
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// Client size that a float window should default to.
        /// </summary>
        protected virtual Size DefaultFloatWindowClientSize
        {
            //
            // Try to size the floating window so that it matches its previous
            // non-floating size.
            //
            get => this.PreviousNonFloatingSize ?? this.Size;
        }
    }
}
