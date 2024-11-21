//
// Copyright 2024 Google LLC
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
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Theme
{
    /// <summary>
    /// Device capabilities.
    /// </summary>
    public class DeviceCapabilities
    {
        private DeviceCapabilities()
        {
            var hdc = NativeMethods.GetDC(IntPtr.Zero);
            try
            {
                //
                // NB. The results of GetDeviceCaps depend
                // on the current DPI awareness mode. If the
                // process is in "unaware" mode, we'll always
                // get 96x96 as LOGPIXELSX/Y.
                //
                this.Dpi =
                    (ushort)NativeMethods.GetDeviceCaps(
                        hdc,
                        NativeMethods.DeviceCap.LOGPIXELSX);
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        /// <summary>
        /// Query device capabilities given the current
        /// DPI awareness mode.
        /// </summary>
        public static DeviceCapabilities Current
        {
            get
            {
                return new DeviceCapabilities();
            }
        }

        /// <summary>
        /// Query device capabilitiesin system DPI mode.
        /// </summary>
        public static DeviceCapabilities System
        {
            get
            {
                if (DpiAwareness.IsSupported)
                {
                    //
                    // Temporarily switch to SystemAware mode
                    // in case this isn't the current mode already.
                    //
                    using (DpiAwareness.EnterThreadMode(DpiAwarenessMode.SystemAware))
                    {
                        return new DeviceCapabilities();
                    }
                }
                else
                {
                    return Current;
                }
            }
        }

        private static int MulDiv(int number, int numerator, int denominator)
        {
            return (int)(((long)number * numerator) / denominator);
        }

        //---------------------------------------------------------------------
        // DPI calculation.
        //---------------------------------------------------------------------

        public const ushort DefaultDpi = 96;

        /// <summary>
        /// Screen DPI.
        /// </summary>
        public ushort Dpi { get; }

        /// <summary>
        /// Check if the screen DPI is higher than the default 96x96.
        /// </summary>
        public bool IsHighDpi
        {
            get => this.Dpi != DefaultDpi;
        }

        /// <summary>
        /// Scale size from default 96x96 to current DPI.
        /// </summary>
        public Size ScaleToDpi(Size size)
        {
            return new Size(
                MulDiv(size.Width, this.Dpi, DeviceCapabilities.DefaultDpi),
                MulDiv(size.Height, this.Dpi, DeviceCapabilities.DefaultDpi));
        }

        /// <summary>
        /// Scale size from default 96x96 to current DPI.
        /// </summary>
        public Rectangle ScaleToDpi(Rectangle rect)
        {
            return new Rectangle(
                rect.Location,
                ScaleToDpi(rect.Size));
        }

        /// <summary>
        /// Scale size from default 96x96 to current DPI.
        /// </summary>
        public Padding ScaleToDpi(Padding padding)
        {
            return new Padding(
                MulDiv(padding.Left, this.Dpi, DeviceCapabilities.DefaultDpi),
                MulDiv(padding.Top, this.Dpi, DeviceCapabilities.DefaultDpi),
                MulDiv(padding.Right, this.Dpi, DeviceCapabilities.DefaultDpi),
                MulDiv(padding.Bottom, this.Dpi, DeviceCapabilities.DefaultDpi));
        }

        /// <summary>
        /// Scale size from default 96x96 to current DPI.
        /// </summary>
        public int ScaleToDpi(int size)
        {
            return MulDiv(size, this.Dpi, DeviceCapabilities.DefaultDpi);
        }

        //---------------------------------------------------------------------
        // P/Invoke.
        //---------------------------------------------------------------------

        internal class NativeMethods
        {
            internal enum DeviceCap : int
            {
                LOGPIXELSX = 88,
                LOGPIXELSY = 90
            }

            [DllImport("user32.dll")]
            public static extern IntPtr GetDC(
                IntPtr hwnd);

            [DllImport("user32.dll")]
            public static extern IntPtr ReleaseDC(
                IntPtr hwnd,
                IntPtr hdc);

            [DllImport("gdi32.dll")]
            public static extern int GetDeviceCaps(
                IntPtr hdc,
                DeviceCap nIndex);
        }
    }
}
