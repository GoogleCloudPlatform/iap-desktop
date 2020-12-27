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

        protected bool IsFullscreen => this.savedBounds != null;

        private Rectangle? savedBounds = null;

        private void fullScreenToggleButton_Click(object sender, EventArgs e)
        {
            try
            {
                SuspendLayout();

                if (this.IsFullscreen)
                {
                    DockTo(this.dockPanel, DockStyle.Fill);
                    this.Bounds = this.savedBounds.Value;
                    this.TopMost = false;
                    this.savedBounds = null;
                }
                else
                {
                    this.savedBounds = this.Bounds;
                    var area = this.BoundsOfAllMonitors;

                    FloatAt(area);

                    var style = GetWindowLongPtr(this.Handle, GWL_STYLE);
                    var style64 = style.ToInt64();
                    style64 &= ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU);

                    var result = SetWindowLongPtr(this.Handle, GWL_STYLE, new IntPtr(style64));
                    Debug.Assert(result != IntPtr.Zero);

                    SetWindowPos(this.Handle, HWND_TOP, area.Left, area.Top, area.Width, area.Height, SWP_FRAMECHANGED);

                    //this.CloseButtonVisible = false;
                    //this.CloseButton = false;
                    ////this.FormBorderStyle = FormBorderStyle.None;
                    //this.Top = area.Top;
                    //this.Left = area.Left;
                    //this.Width = area.Width;
                    //this.Height = area.Height;
                    //this.TopMost = true;
                }
            }
            finally
            {
                ResumeLayout();
            }
        }

        private const int GWL_STYLE = -16;
        private const int HWND_TOP = 0;
        private const int SWP_FRAMECHANGED = 0x0020;
        private const uint WS_MAXIMIZEBOX = 0x00010000;
        private const uint WS_MINIMIZEBOX = 0x00020000;
        private const uint WS_THICKFRAME = 0x00040000;
        private const uint WS_SYSMENU = 0x00080000;
        private const uint WS_CAPTION = 0x00C00000;



        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowPos
            (IntPtr hWnd, 
            int hWndInsertAfter, 
            int x, 
            int y, 
            int cx, 
            int cy, 
            int wFlags);


        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        // This static method is required because Win32 does not support
        // GetWindowLongPtr directly
        public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return GetWindowLongPtr32(hWnd, nIndex);
        }





        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern IntPtr SetWindowLongPtr32(
            IntPtr hWnd,
            int nIndex,
            IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(
            IntPtr hWnd,
            int nIndex,
            IntPtr dwNewLong);

        // This static method is required because Win32 does not support
        // SetWindowLongPtr directly
        public static IntPtr SetWindowLongPtr(
            IntPtr hWnd,
            int nIndex,
            IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return SetWindowLongPtr32(hWnd, nIndex, dwNewLong);
        }
    }
}
