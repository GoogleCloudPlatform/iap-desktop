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
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Theme
{
    public class DeviceCapabilities
    {
        //---------------------------------------------------------------------
        // DPI calculation.
        //---------------------------------------------------------------------

        public const ushort DefaultDpi = 96;

        public ushort SystemDpi { get; }

        public bool IsHighDpiEnabled
        {
            get => this.SystemDpi != DefaultDpi;
        }

        public static int MulDiv(int number, int numerator, int denominator)
        {
            return (int)(((long)number * numerator) / denominator);
        }

        public Size ScaleToSystemDpi(Size size)
        {
            return new Size(
                MulDiv(size.Width, this.SystemDpi, DeviceCapabilities.DefaultDpi),
                MulDiv(size.Height, this.SystemDpi, DeviceCapabilities.DefaultDpi));
        }

        public Rectangle ScaleToSystemDpi(Rectangle rect)
        {
            return new Rectangle(
                rect.Location,
                ScaleToSystemDpi(rect.Size));
        }

        public Padding ScaleToSystemDpi(Padding padding)
        {
            return new Padding(
                MulDiv(padding.Left, this.SystemDpi, DeviceCapabilities.DefaultDpi),
                MulDiv(padding.Top, this.SystemDpi, DeviceCapabilities.DefaultDpi),
                MulDiv(padding.Right, this.SystemDpi, DeviceCapabilities.DefaultDpi),
                MulDiv(padding.Bottom, this.SystemDpi, DeviceCapabilities.DefaultDpi));
        }

        public int ScaleToSystemDpi(int size)
        {
            return MulDiv(size, this.SystemDpi, DeviceCapabilities.DefaultDpi);
        }

        //---------------------------------------------------------------------
        // Factory method.
        //---------------------------------------------------------------------

        private DeviceCapabilities(ushort systemDpi)
        {
            this.SystemDpi = systemDpi;
        }

        public static DeviceCapabilities GetScreenCapabilities()
        {
            var hdc = NativeMethods.GetDC(IntPtr.Zero);
            try
            {
                return new DeviceCapabilities(
                    (ushort)NativeMethods.GetDeviceCaps(
                        hdc,
                        NativeMethods.DeviceCap.LOGPIXELSX));
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
            }
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
