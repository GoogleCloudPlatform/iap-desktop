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

using Google.Solutions.Common.Interop;
using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Mvvm.Drawing;
using Google.Solutions.Mvvm.Interop;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

#pragma warning disable CA1822 // Mark members as static

namespace Google.Solutions.Mvvm.Theme
{
    /// <summary>
    /// Theming rules for using dark mode.
    /// </summary>
    public class WindowsRuleSet : ControlTheme.IRuleSet
    {
        /// <summary>
        /// Check if this application uses dark mode.
        /// </summary>
        public bool IsDarkModeEnabled { get; }

        public WindowsRuleSet(bool darkMode)
        {
            Debug.Assert(!darkMode || SystemTheme.IsDarkModeSupported);

            this.IsDarkModeEnabled = darkMode;
            if (darkMode)
            {
                //
                // Force Win32 controls to use dark mode.
                //
                _ = NativeMethods.SetPreferredAppMode(NativeMethods.APPMODE.FORCEDARK);
            }
        }

        //---------------------------------------------------------------------
        // Theming rules.
        //---------------------------------------------------------------------

        /// <summary>
        /// Use dark title bar for top-level windows, see
        /// https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/apply-windows-themes
        /// </summary>
        /// <param name="form"></param>
        private void StyleTitleBar(Form form)
        {
            if (!this.IsDarkModeEnabled || !form.TopLevel)
            {
                return;
            }

            //
            // Use dark title bar, see
            // https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/apply-windows-themes
            //
            var darkMode = 1;
            var hr = NativeMethods.DwmSetWindowAttribute(
                form.Handle,
                NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE,
                ref darkMode,
                sizeof(int));
            if (hr != HRESULT.S_OK)
            {
                throw new Win32Exception(
                    "Updating window attributes failed");
            }
        }

        /// <summary>
        /// Opt-in window to use dark mode.
        /// </summary>
        private void StyleControl(Control control)
        {
            if (this.IsDarkModeEnabled)
            {
                NativeMethods.AllowDarkModeForWindow(control.Handle, true);
            }
        }


        private void StyleTreeView(TreeView treeView)
        {
            treeView.HotTracking = true;

            //
            // NB. When called after AllowDarkModeForWindow, this also applies
            // dark mode-style scrollbars, etc.
            //
            _ = NativeMethods.SetWindowTheme(treeView.Handle, "Explorer", null);
        }

        private void StyleListView(ListView listView)
        {
            listView.HotTracking = false;

            //
            // NB. When called after AllowDarkModeForWindow, this also applies
            // dark mode-style scrollbars, etc.
            //
            _ = NativeMethods.SetWindowTheme(listView.Handle, "Explorer", null);

            var designMode = (LicenseManager.UsageMode == LicenseUsageMode.Designtime);
            if (this.IsDarkModeEnabled && !designMode)
            {
                //
                // In dark mode, we also need to apply a theme to the header,
                // otherwise it stays in light mode. Note that this doesn't
                // set the header text color correctly yet.
                //
                var headerHandle = listView.GetHeaderHandle();
                Debug.Assert(headerHandle != IntPtr.Zero);

                NativeMethods.AllowDarkModeForWindow(headerHandle, true);
                _ = NativeMethods.SetWindowTheme(headerHandle, "ItemsView", null);

                //
                // Subclass the list view (not the header) to adjust the text color
                // in the column header. Adapted from
                // https://github.com/ysc3839/win32-darkmode/blob/master/win32-darkmode/ListViewUtil.h
                //
                var subclass = new SubclassCallback(listView, (ref Message m) =>
                {
                    switch (m.Msg)
                    {
                        case NativeMethods.WM_NOTIFY:
                            var hdr = Marshal.PtrToStructure<NativeMethods.NMHDR>(m.LParam);
                            if (hdr.code == NativeMethods.NM_CUSTOMDRAW)
                            {
                                var custDraw = Marshal.PtrToStructure<NativeMethods.NMCUSTOMDRAW>(m.LParam);

                                switch (custDraw.dwDrawStage)
                                {
                                    case NativeMethods.CDDS_PREPAINT:
                                        m.Result = new IntPtr(NativeMethods.CDRF_NOTIFYITEMDRAW);
                                        break;

                                    case NativeMethods.CDDS_ITEMPREPAINT:
                                        _ = NativeMethods.SetTextColor(
                                            custDraw.hdc,
                                            listView.ForeColor.ToCOLORREF());
                                        m.Result = new IntPtr(NativeMethods.CDRF_DODEFAULT);
                                        break;
                                }
                            }
                            else if (hdr.code == NativeMethods.HDN_ITEMCHANGEDW)
                            {
                                //
                                // When resizing a column header, the list isn't redrawn
                                // automatically. Thus, force a redraw.
                                //
                                listView.Invalidate();
                            }
                            break;

                        default:
                            SubclassCallback.DefaultWndProc(ref m);
                            break;
                    }
                });

                subclass.UnhandledException += (_, args) =>
                    Debug.Fail(args.FullMessage());
            }
        }

        private void StyleTextBox(TextBox text)
        {
            _ = NativeMethods.SetWindowTheme(text.Handle, "Explorer", null);
        }

        private void StyleComboBox(ComboBox combo)
        {
            if (this.IsDarkModeEnabled)
            {
                _ = NativeMethods.SetWindowTheme(combo.Handle, "CFD", null);
            }
        }

        private void StyleScrollbar(ScrollBar bar)
        {
            if (this.IsDarkModeEnabled)
            {
                _ = NativeMethods.SetWindowTheme(bar.Handle, "Explorer", null);
            }
        }

        private static void ResetWindowTheme(Control control)
        {
            _ = NativeMethods.SetWindowTheme(control.Handle, string.Empty, string.Empty);
        }

        //---------------------------------------------------------------------
        // IRuleSet
        //---------------------------------------------------------------------

        /// <summary>
        /// Register rules.
        /// </summary>
        public void AddRules(ControlTheme controlTheme)
        {
            controlTheme.ExpectNotNull(nameof(controlTheme));

            controlTheme.AddRule<Form>(
                c => StyleTitleBar(c),
                ControlTheme.Options.ApplyWhenHandleCreated);
            controlTheme.AddRule<Control>(
                c => StyleControl(c),
                ControlTheme.Options.ApplyWhenHandleCreated);
            controlTheme.AddRule<TreeView>(
                c => StyleTreeView(c),
                ControlTheme.Options.ApplyWhenHandleCreated);
            controlTheme.AddRule<ListView>(
                c => StyleListView(c),
                ControlTheme.Options.ApplyWhenHandleCreated);
            controlTheme.AddRule<TextBox>(c => StyleTextBox(c));
            controlTheme.AddRule<ComboBox>(c => StyleComboBox(c));
            controlTheme.AddRule<ScrollBar>(c => StyleScrollbar(c));
        }

        //---------------------------------------------------------------------
        // P/Invoke.
        //
        // NB. Most APIs are undocumented. See
        // https://github.com/microsoft/WindowsAppSDK/issues/41 for details.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

            public const int WM_NOTIFY = 0x004E;
            public const int NM_CUSTOMDRAW = -12;
            public const int CDDS_PREPAINT = 1;
            public const int CDDS_ITEM = 0x10000;
            public const int CDDS_ITEMPREPAINT = CDDS_ITEM | CDDS_PREPAINT;
            public const int CDRF_NOTIFYITEMDRAW = 0x20;
            public const int CDRF_DODEFAULT = 0x00000000;
            public const int HDN_ITEMCHANGEDW = -321;

            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct NMHDR
            {
                public IntPtr hwndFrom;
                public IntPtr idFrom;
                public int code;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct NMCUSTOMDRAW
            {
                public NMHDR hdr;
                public int dwDrawStage;
                public IntPtr hdc;
                public RECT rc;
                public IntPtr dwItemSpec;
                public int uItemState;
                public IntPtr lItemlParam;
            }

            public enum APPMODE : int
            {
                DEFAULT = 0,
                ALLOWDARK = 1,
                FORCEDARK = 2,
                FORCELIGHT = 3,
                MAX = 4
            }

            [DllImport("uxtheme.dll", EntryPoint = "#133")]
            public static extern bool AllowDarkModeForWindow(IntPtr hWnd, bool allow);

            [DllImport("uxtheme.dll", EntryPoint = "#135")]
            public static extern int SetPreferredAppMode(APPMODE appMode);

            [DllImport("dwmapi.dll")]
            public static extern HRESULT DwmSetWindowAttribute(
                IntPtr hwnd,
                int attr,
                ref int attrValue,
                int attrSize);

            [DllImport("uxtheme", ExactSpelling = true, CharSet = CharSet.Unicode)]
            public static extern int SetWindowTheme(
                IntPtr hWnd,
                string textSubAppName,
                string? textSubIdList);


            [DllImport("gdi32.dll")]
            public static extern uint SetTextColor(IntPtr hdc, uint color);
        }
    }
}
