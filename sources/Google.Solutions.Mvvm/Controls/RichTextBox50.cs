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
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    /// <summary>
    /// Rich text box that uses RICHEDIT50W.
    /// </summary>
    public class RichTextBox50 : RichTextBox
    {
        //---------------------------------------------------------------------
        // Support for friendly links
        //
        // The base class has a bug that causes links to not
        // be clickable under certain conditions, see
        // https://stackoverflow.com/a/56938772/4372 for details.
        //---------------------------------------------------------------------

        private static unsafe NativeMethods.ENLINK ConvertFromENLINK64(NativeMethods.ENLINK64 es64)
        {
            var es = new NativeMethods.ENLINK();

            fixed (byte* es64p = &es64.contents[0])
            {
                es.nmhdr = new NativeMethods.NMHDR();
                es.charrange = new NativeMethods.CHARRANGE();

                es.nmhdr.hwndFrom = Marshal.ReadIntPtr((IntPtr)es64p);
                es.nmhdr.idFrom = Marshal.ReadIntPtr((IntPtr)(es64p + 8));
                es.nmhdr.code = Marshal.ReadInt32((IntPtr)(es64p + 16));
                es.msg = Marshal.ReadInt32((IntPtr)(es64p + 24));
                es.wParam = Marshal.ReadIntPtr((IntPtr)(es64p + 28));
                es.lParam = Marshal.ReadIntPtr((IntPtr)(es64p + 36));
                es.charrange.cpMin = Marshal.ReadInt32((IntPtr)(es64p + 44));
                es.charrange.cpMax = Marshal.ReadInt32((IntPtr)(es64p + 48));
            }

            return es;
        }

        private string CharRangeToString(NativeMethods.CHARRANGE c)
        {
            Debug.Assert(c.cpMax > c.cpMin);
            var txrg = new NativeMethods.TEXTRANGE() { chrg = c };

            //
            // NB. c.cpMax can be greater than Text.Length if using friendly links
            // with RichEdit50. so that check is not valid.  
            //
            // instead of the hack above, first check that the number of characters is positive 
            // and then use the result of sending EM_GETTEXTRANGE to handle the 
            // possibility of Text.Length < c.cpMax
            // 

            var numCharacters = c.cpMax - c.cpMin + 1; // +1 for null termination
            if (numCharacters > 0)
            {
                using (var buffer = LocalAllocSafeHandle.LocalAlloc((uint)numCharacters * 2))
                {
                    txrg.lpstrText = buffer.DangerousGetHandle();

                    var len = NativeMethods.SendMessage(
                        this.Handle,
                        NativeMethods.EM_GETTEXTRANGE,
                        IntPtr.Zero,
                        txrg);
                    if (len != IntPtr.Zero)
                    {
                        var s = Marshal.PtrToStringUni(buffer.DangerousGetHandle());
                        Debug.Assert(!string.IsNullOrEmpty(s));
                        return s;
                    }
                }
            }

            return string.Empty;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_REFLECT + NativeMethods.WM_NOTIFY)
            {
                var hdr = (NativeMethods.NMHDR)m.GetLParam(typeof(NativeMethods.NMHDR));
                if (hdr.code == NativeMethods.EN_LINK)
                {
                    NativeMethods.ENLINK lnk;
                    if (IntPtr.Size == 4)
                    {
                        lnk = (NativeMethods.ENLINK)m.GetLParam(typeof(NativeMethods.ENLINK));
                    }
                    else
                    {
                        lnk = ConvertFromENLINK64(
                            (NativeMethods.ENLINK64)m.GetLParam(typeof(NativeMethods.ENLINK64)));
                    }

                    if (lnk.msg == NativeMethods.WM_LBUTTONDOWN)
                    {
                        var href = CharRangeToString(lnk.charrange);
                        if (!string.IsNullOrEmpty(href))
                        {
                            OnLinkClicked(new LinkClickedEventArgs(href));
                        }

                        m.Result = new IntPtr(1);
                        return;
                    }
                }
            }

            base.WndProc(ref m);
        }

        private static class NativeMethods
        {
            internal const int EN_LINK = 0x70B;
            internal const int WM_NOTIFY = 0x4E;
            internal const int WM_USER = 0x400;
            internal const int WM_REFLECT = WM_USER + 0x1C00;
            internal const int WM_LBUTTONDOWN = 0x201;
            internal const int EM_GETTEXTRANGE = WM_USER + 75;

            public struct NMHDR
            {
                public IntPtr hwndFrom;
                public IntPtr idFrom;
                public int code;
            }

            [StructLayout(LayoutKind.Sequential)]
            public class ENLINK
            {
                public NMHDR nmhdr;
                public int msg = 0;
                public IntPtr wParam = IntPtr.Zero;
                public IntPtr lParam = IntPtr.Zero;
                public CHARRANGE charrange = null;
            }

            [StructLayout(LayoutKind.Sequential)]
            public class ENLINK64
            {
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 56)]
                public byte[] contents = new byte[56];
            }

            [StructLayout(LayoutKind.Sequential)]
            public class CHARRANGE
            {
                public int cpMin;
                public int cpMax;
            }

            [StructLayout(LayoutKind.Sequential)]
            public class TEXTRANGE
            {
                public CHARRANGE chrg;

                // NB. Allocated by caller, zero terminated by RichEdit
                public IntPtr lpstrText;
            }

            [DllImport("user32.dll")]
            public static extern IntPtr SendMessage(
                IntPtr hWnd,
                int msg,
                IntPtr wparam,
                TEXTRANGE lparam);

            [DllImport(
                "kernel32.dll",
                EntryPoint = "LoadLibraryW",
                CharSet = CharSet.Unicode,
                SetLastError = true)]
            internal static extern IntPtr LoadLibraryW(string file);
        }
    }
}
