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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.Mvvm.Interop;
using Google.Solutions.Mvvm.Theme;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Theme
{
    internal class VSThemeExtensions
    {
        //---------------------------------------------------------------------
        // Tool strips.
        //---------------------------------------------------------------------

        internal class ToolStripRenderer : VisualStudioToolStripRenderer
        {
            private readonly DockPanelColorPalette palette;

            public ToolStripRenderer(DockPanelColorPalette palette) : base(palette)
            {
                this.palette = palette;
                base.UseGlassOnMenuStrip = false;
            }

            protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
            {
                //
                // The base class doesn't adjust the arrow color.
                // That's okay in light mode, but makes arrows almost invisible
                // in dark mode.
                //
                if (e.Item is ToolStripMenuItem item && item != null)
                {
                    //
                    // Sub-menu.
                    //
                    e.ArrowColor = this.palette.CommandBarMenuPopupDefault.Arrow;
                }
                else if (e.Item is ToolStripDropDownButton &&
                         e.Item.Owner is ToolStrip toolStrip)
                {
                    if (toolStrip is StatusStrip)
                    {
                        //
                        // DropDownButton main window status strip.
                        //
                        e.ArrowColor = this.palette.MainWindowStatusBarDefault.Text;
                    }
                    else
                    {
                        //
                        // DropDownButton in tool strip.
                        //
                        e.ArrowColor = this.palette.CommandBarToolbarButtonDefault.Arrow;
                    }
                }

                base.OnRenderArrow(e);
            }
        }

        //---------------------------------------------------------------------
        // Float window.
        //---------------------------------------------------------------------

        private class MinimizableFloatWindow : FloatWindow
        {
            private readonly IControlTheme? theme;

            public MinimizableFloatWindow(
                DockPanel dockPanel,
                DockPane pane,
                IControlTheme? theme)
                : base(dockPanel, pane)
            {
                this.theme = theme;
            }

            public MinimizableFloatWindow(
                DockPanel dockPanel,
                DockPane pane,
                Rectangle bounds,
                IControlTheme? theme)
                : base(dockPanel, pane, bounds)
            {
                this.theme = theme;
            }

            public void ApplyTheme()
            {
                this.theme?.ApplyTo(this);
            }

            protected override void WndProc(ref Message m)
            {
                if (base.IsDisposed)
                {
                    return;
                }

                //
                // The base classes implementation doesn't handle clicks on the
                // minimize button properly, see 
                // https://github.com/dockpanelsuite/dockpanelsuite/issues/526..
                //
                if (m.Msg == (int)WindowMessage.WM_NCLBUTTONDOWN &&
                    m.WParam.ToInt32() == NativeMethods.HTREDUCE)
                {
                    //
                    // Eat this message so that the base class can't misinterpret it
                    // as a click on the title bar.
                    //
                }
                else if (m.Msg == (int)WindowMessage.WM_NCLBUTTONUP &&
                    m.WParam.ToInt32() == NativeMethods.HTREDUCE)
                {
                    //
                    // Minimize window.
                    //
                    NativeMethods.SendMessage(
                        this.Handle,
                        (int)WindowMessage.WM_SYSCOMMAND,
                        new IntPtr(NativeMethods.SC_MINIMIZE),
                        IntPtr.Zero);
                }
                else
                {
                    base.WndProc(ref m);
                }
            }

            protected override void OnLayout(LayoutEventArgs levent)
            {
                base.OnLayout(levent);

                //
                // When a float window is split, the base class resets
                // the icon and sets the title to " ".
                //
                // When that happens, apply the standard title and
                // icon again so that we avoid showing a windows with a
                // standard icon and empty title.
                //
                if (string.IsNullOrWhiteSpace(this.Text))
                {
                    this.Text = Install.ProductName;
                    this.Icon = Install.ProductIcon;
                }
            }

            protected override void Dispose(bool disposing)
            {
                try
                {
                    base.Dispose(disposing);
                }
                catch (InvalidOperationException)
                {
                    //
                    // b/262842025: When the parent window is closed, it requests float 
                    // windows to dispose by sending it a WM_USER+1 message (see FloatWindow 
                    // in DockPanelSuite).
                    //
                    // This WM_USER+1 message is handled asynchronously. Thus, the parent 
                    // window's handle might have already been destroyed when this window is
                    // dispatching the message. However, the base class, under some
                    // circumstances, touches the main window handle, triggering
                    // an exception. The exception is benign as we're disposing anyway,
                    // so ignore it.
                    // 
                    Debug.Assert(false, "Disposing float window failed");
                }
            }
        }

        internal class FloatWindowFactory : DockPanelExtender.IFloatWindowFactory
        {
            public IControlTheme? Theme { get; set; }

            public FloatWindow CreateFloatWindow(DockPanel dockPanel, DockPane pane, Rectangle bounds)
            {
                var window = new MinimizableFloatWindow(dockPanel, pane, bounds, this.Theme);
                window.ApplyTheme();
                return window;
            }

            public FloatWindow CreateFloatWindow(DockPanel dockPanel, DockPane pane)
            {
                var window = new MinimizableFloatWindow(dockPanel, pane, this.Theme);
                window.ApplyTheme();
                return window;
            }
        }

        //---------------------------------------------------------------------
        // DockPaneFactory.
        //---------------------------------------------------------------------

        internal class DockPaneFactory : DockPanelExtender.IDockPaneFactory
        {
            private readonly DockPanelExtender.IDockPaneFactory factory;

            public DockPaneFactory(DockPanelExtender.IDockPaneFactory factory)
            {
                Debug.Assert(factory != null);
                this.factory = factory.ExpectNotNull(nameof(factory));
            }

            public DockPane CreateDockPane(
                IDockContent content,
                DockState visibleState,
                bool show)
            {
                return this.factory.CreateDockPane(content, visibleState, show);
            }

            public DockPane CreateDockPane(
                IDockContent content,
                FloatWindow floatWindow,
                bool show)
            {
                return this.factory.CreateDockPane(content, floatWindow, show);
            }

            public DockPane CreateDockPane(
                IDockContent content,
                DockPane prevPane,
                DockAlignment alignment,
                double proportion,
                bool show)
            {
                return this.factory.CreateDockPane(content, prevPane, alignment, proportion, show);
            }

            public DockPane CreateDockPane(
                IDockContent content,
                Rectangle floatWindowBounds,
                bool show)
            {
                if (content is DocumentWindow docWindow)
                {
                    //
                    // Maintain the original client size. That's particularly
                    // important for RDP window as resizing is slow and expensive.
                    //
                    var form = docWindow.DockHandler.DockPanel.FindForm();
                    var nonClientOverhead = new Size
                    {
                        Width = form.Width - form.ClientRectangle.Width,
                        Height = form.Height - form.ClientRectangle.Height
                    };

                    var pane = this.factory.CreateDockPane(
                        content,
                        new Rectangle(
                            docWindow.Bounds.Location,
                            docWindow.Bounds.Size + nonClientOverhead),
                        show);

                    Debug.Assert(pane.FloatWindow != null);

                    //
                    // Make this a first-class window.
                    //
                    pane.FloatWindow!.FormBorderStyle = FormBorderStyle.Sizable;
                    pane.FloatWindow.ShowInTaskbar = true;
                    pane.FloatWindow.Owner = null;

                    //
                    // Setting the properties above makes Windows forget about
                    // whether the window was supposed to use light or dark mode.
                    // Reapply theme to restore theming consistency.
                    //
                    ((MinimizableFloatWindow)pane.FloatWindow).ApplyTheme();

                    return pane;
                }
                else
                {
                    return this.factory.CreateDockPane(content, floatWindowBounds, show);
                }
            }
        }

        //---------------------------------------------------------------------
        // P/Invoke.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            internal const int HTREDUCE = 8;
            internal const int SC_MINIMIZE = 0xF020;

            [DllImport("user32.dll")]
            internal static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
        }
    }
}
