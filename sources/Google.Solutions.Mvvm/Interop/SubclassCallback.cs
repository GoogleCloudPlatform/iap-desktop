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

using Google.Solutions.Common.Util;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Interop
{
    /// <summary>
    /// Callback for subclassing external windows. 
    /// </summary>
    public sealed class SubclassCallback : IDisposable
    {
        private readonly NativeMethods.SubclassProcedure hookProc;

        private readonly IntPtr hookProcPtr;
        private readonly WndProc wndProc;

        public delegate void WndProc(ref Message m);

        private IntPtr Callback(
            IntPtr hWnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            UIntPtr uIdSubclass,
            UIntPtr dwRefData)
        {
            Debug.Assert(!this.IsDisposed);

            try
            {
                var m = Message.Create(hWnd, msg, wParam, lParam);
                this.wndProc(ref m);
                return m.Result;
            }
            catch (Exception e)
            {
                this.UnhandledException?.Invoke(this, e);
                return IntPtr.Zero;
            }
            finally
            {
                if (msg == NativeMethods.WM_NCDESTROY)
                {
                    Dispose();
                }
            }
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public IntPtr WindowHandle { get; private set; }

        public event EventHandler<Exception>? UnhandledException;
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Install a window hook for subclassing.
        /// </summary>
        /// <param name="windowHandle">Window handle</param>
        /// <param name="owningComponent">Component that owns the window</param>
        /// <param name="wndProc">The window's WndProc</param>
        public SubclassCallback(
            IntPtr windowHandle,
            Component owningComponent,
            WndProc wndProc)
        {
            if (windowHandle == IntPtr.Zero)
            {
                throw new ArgumentException("Control has no handle");
            }

            this.WindowHandle = windowHandle;
            this.wndProc = wndProc.ExpectNotNull(nameof(wndProc));

            //
            // Install a hook for this particular window.
            //
            this.hookProc = new NativeMethods.SubclassProcedure(Callback);
            this.hookProcPtr = Marshal.GetFunctionPointerForDelegate(this.hookProc);

            //
            // Tie the lifetime to the control. This ensures that:
            // - this object (and crucially, the delegate callback) is
            //   being prevented from GC collecting
            // - our own resources are being cleaned up.
            //
            owningComponent.Disposed += (_, __) => Dispose();

            if (!NativeMethods.SetWindowSubclass(
                windowHandle,
                this.hookProcPtr,
                UIntPtr.Zero,
                UIntPtr.Zero))
            {
                throw new Win32Exception("Installing the window hook failed");
            }
        }

        /// <summary>
        /// Install a window hook for subclassing.
        /// </summary>
        /// <param name="control">Control to subclass</param>
        /// <param name="wndProc">The control's WndProc</param>
        public SubclassCallback(
            Control control,
            WndProc wndProc)
            : this(control.Handle, control, wndProc)
        {
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            if (!this.IsDisposed)
            {
                var hookRemoved = NativeMethods.RemoveWindowSubclass(
                    this.WindowHandle,
                    this.hookProcPtr,
                    UIntPtr.Zero);
                Debug.Assert(hookRemoved);

                this.IsDisposed = true;
            }
        }

        //---------------------------------------------------------------------
        // Statics.
        //---------------------------------------------------------------------

        public static void DefaultWndProc(ref Message m)
        {
            //
            // Defer to default window procedure.
            //
            m.Result = NativeMethods.DefSubclassProc(m.HWnd, m.Msg, m.WParam, m.LParam);
        }

        //---------------------------------------------------------------------
        // P/Invoke.
        //---------------------------------------------------------------------

        private static partial class NativeMethods
        {
            public const int WM_NCDESTROY = 0x82;

            public delegate IntPtr SubclassProcedure(
                IntPtr hWnd,
                int msg,
                IntPtr wParam,
                IntPtr lParam,
                UIntPtr uIdSubclass,
                UIntPtr dwRefData
            );

            [DllImport("comctl32.dll", ExactSpelling = true)]
            public static extern bool SetWindowSubclass(
                IntPtr hWnd,
                IntPtr pfnSubclass,
                UIntPtr uIdSubclass,
                UIntPtr dwRefData);

            [DllImport("comctl32.dll", ExactSpelling = true)]
            public static extern bool RemoveWindowSubclass(
                IntPtr hWnd,
                IntPtr pfnSubclass,
                UIntPtr uIdSubclass);

            [DllImport("comctl32.dll", ExactSpelling = true)]
            public static extern IntPtr DefSubclassProc(
                IntPtr hWnd,
                int msg,
                IntPtr wParam,
                IntPtr lParam);
        }
    }
}
