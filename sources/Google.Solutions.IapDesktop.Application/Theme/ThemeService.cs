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

using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.Mvvm.Theme;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Theme
{
    /// <summary>
    /// Applies themes to controls and dialogs.
    /// </summary>
    public interface IThemeService
    {
        /// <summary>
        /// Theme for dialogs and other secondary windows.
        /// </summary>
        IControlTheme DialogTheme { get; }

        /// <summary>
        /// Theme for tool windows, docked or undocked.
        /// </summary>
        IControlTheme ToolWindowTheme { get; }

        /// <summary>
        /// Theme for the main window.
        /// </summary>
        IControlTheme MainWindowTheme { get; }

        /// <summary>
        /// Theme for the docking suite.
        /// </summary>
        ThemeBase DockPanelTheme { get; }
    }

    public class ThemeService : IThemeService
    {
        public ThemeService(ThemeSettingsRepository themeSettingsRepository)
        {
            var settings = themeSettingsRepository.GetSettings();

            WindowsTheme windowsTheme;
            VSTheme vsTheme;
            switch (settings.Theme.EnumValue)
            {
                case ThemeSettings.ApplicationTheme.System:
                    //
                    // Use same mode as Windows.
                    //
                    windowsTheme = WindowsTheme.GetSystemTheme();
                    vsTheme = windowsTheme.IsDark
                        ? VSTheme.GetDarkTheme()
                        : VSTheme.GetLightTheme();
                    break;

                case ThemeSettings.ApplicationTheme.Dark:
                    //
                    // Use dark mode, even if Windows uses light mode.
                    //
                    windowsTheme = WindowsTheme.GetDarkTheme();
                    vsTheme = VSTheme.GetDarkTheme();
                    break;

                default:
                    //
                    // Use safe defaults that also work on downlevel
                    // versions of Windows.
                    //
                    windowsTheme = WindowsTheme.GetDefaultTheme();
                    vsTheme = VSTheme.GetLightTheme();
                    break;
            }

            //
            // Apply the resulting theme to the different kinds of windows we have.
            //
            this.DockPanelTheme = vsTheme;
            this.DockPanelTheme.Extender.FloatWindowFactory = new FloatWindowFactory();
            this.DockPanelTheme.Extender.DockPaneFactory =
                new DockPaneFactory(this.DockPanelTheme.Extender.DockPaneFactory);

            this.DialogTheme = new ControlTheme(windowsTheme);
            this.ToolWindowTheme = new ToolWindowTheme(windowsTheme, vsTheme);
            this.MainWindowTheme = new MainWindowTheme(windowsTheme, vsTheme);
        }

        //---------------------------------------------------------------------
        // ITheme.
        //---------------------------------------------------------------------

        public ThemeBase DockPanelTheme { get; }
        public IControlTheme DialogTheme { get; }
        public IControlTheme ToolWindowTheme { get; }
        public IControlTheme MainWindowTheme { get; }

        //---------------------------------------------------------------------
        // DockPaneFactory.
        //---------------------------------------------------------------------

        private class MinimizableFloatWindow : FloatWindow
        {
            public MinimizableFloatWindow(DockPanel dockPanel, DockPane pane)
                : base(dockPanel, pane)
            {
            }

            public MinimizableFloatWindow(DockPanel dockPanel, DockPane pane, Rectangle bounds)
                : base(dockPanel, pane, bounds)
            {
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
                    // windows to dispose by sending it a WM_USER+1 message (see FloátWindow 
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

        private class FloatWindowFactory : DockPanelExtender.IFloatWindowFactory
        {
            public FloatWindow CreateFloatWindow(DockPanel dockPanel, DockPane pane, Rectangle bounds)
            {
                return new MinimizableFloatWindow(dockPanel, pane, bounds);
            }

            public FloatWindow CreateFloatWindow(DockPanel dockPanel, DockPane pane)
            {
                return new MinimizableFloatWindow(dockPanel, pane);
            }
        }

        //---------------------------------------------------------------------
        // DockPaneFactory.
        //---------------------------------------------------------------------

        private class DockPaneFactory : DockPanelExtender.IDockPaneFactory
        {
            private readonly DockPanelExtender.IDockPaneFactory factory;

            public DockPaneFactory(DockPanelExtender.IDockPaneFactory factory)
            {
                Debug.Assert(factory != null);
                this.factory = factory;
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
                    pane.FloatWindow.FormBorderStyle = FormBorderStyle.Sizable;
                    pane.FloatWindow.ShowInTaskbar = true;
                    pane.FloatWindow.Owner = null;

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
