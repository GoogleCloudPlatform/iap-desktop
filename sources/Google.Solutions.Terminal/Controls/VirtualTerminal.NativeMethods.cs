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

using Google.Solutions.Mvvm.Interop;
using Google.Solutions.Platform.Interop;
using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.Terminal.Controls
{
    public partial class VirtualTerminal
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct TilPoint
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TilSize
        {
            public int X;
            public int Y;
        }

        public enum CaretStyle : uint
        {
            BlinkingBlock = 0,
            BlinkingBlockDefault = 1,
            SteadyBlock = 2,
            BlinkingUnderline = 3,
            SteadyUnderline = 4,
            BlinkingBar = 5,
            SteadyBar = 6,
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TerminalTheme
        {
            public uint DefaultBackground;
            public uint DefaultForeground;
            public uint DefaultSelectionBackground;
            public float SelectionBackgroundAlpha;
            public CaretStyle CursorStyle;

            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U4, SizeConst = 16)]
            public uint[] ColorTable;
        }

        private struct WmKeyUpDownParams
        {
            public ushort ScanCode;
            public ushort Flags;
            public ushort VirtualKey;

            public WmKeyUpDownParams(Message m)
            {
                Debug.Assert(
                    m.Msg == (int)WindowMessage.WM_SYSKEYUP ||
                    m.Msg == (int)WindowMessage.WM_SYSKEYDOWN ||
                    m.Msg == (int)WindowMessage.WM_KEYUP ||
                    m.Msg == (int)WindowMessage.WM_KEYDOWN ||
                    m.Msg == (int)WindowMessage.WM_CHAR);

                var scanCodeAndFlags = (((ulong)m.LParam) & 0xFFFF0000) >> 16;

                this.ScanCode = (ushort)(scanCodeAndFlags & 0x00FFu);
                this.Flags = (ushort)(scanCodeAndFlags & 0xFF00u);
                this.VirtualKey = (ushort)m.WParam;
            }
        }

        private struct WmCharParams
        {
            public ushort ScanCode;
            public ushort Flags;
            public char Character;

            public WmCharParams(Message m)
            {
                Debug.Assert(m.Msg == (int)WindowMessage.WM_CHAR);

                var keyParams = new WmKeyUpDownParams(m);
                this.ScanCode = keyParams.ScanCode;
                this.Flags = keyParams.Flags;
                this.Character = (char)keyParams.VirtualKey;
            }
        }

        private static class NativeMethods
        {
            private const string TerminalCore = "Microsoft.Terminal.Control.dll";
            private const string User32 = "user32.dll";

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate void ScrollCallback(int viewTop, int viewHeight, int bufferSize);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate void WriteCallback([In, MarshalAs(UnmanagedType.LPWStr)] string data);

            //---------------------------------------------------------------------
            // Create/destroy.
            //---------------------------------------------------------------------

            [DllImport(TerminalCore, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern HRESULT CreateTerminal(
                IntPtr parent,
                out IntPtr hwnd,
                out TerminalSafeHandle terminal);

            [DllImport(TerminalCore, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern void DestroyTerminal(IntPtr terminal);

            //---------------------------------------------------------------------
            // I/O.
            //---------------------------------------------------------------------

            [DllImport(TerminalCore, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern void TerminalSendOutput(
                TerminalSafeHandle terminal,
                string lpdata);


            [DllImport(TerminalCore, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern void TerminalRegisterWriteCallback(
                TerminalSafeHandle terminal,
                [MarshalAs(UnmanagedType.FunctionPtr)] WriteCallback callback);


            [DllImport(TerminalCore, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern void TerminalSendKeyEvent(
                TerminalSafeHandle terminal,
                ushort vkey,
                ushort scanCode,
                ushort flags,
                bool keyDown);

            [DllImport(TerminalCore, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern void TerminalSendCharEvent(
                TerminalSafeHandle terminal,
                char ch,
                ushort scanCode,
                ushort flags);

            //---------------------------------------------------------------------
            // Resizing.
            //---------------------------------------------------------------------

            [DllImport(TerminalCore, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern HRESULT TerminalTriggerResize(
                TerminalSafeHandle terminal,
                int width,
                int height,
                out TilSize dimensions);

            [DllImport(TerminalCore, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern HRESULT TerminalTriggerResizeWithDimension(
                TerminalSafeHandle terminal,
                TilSize dimensions,
                out TilSize dimensionsInPixels);

            [DllImport(TerminalCore, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern HRESULT TerminalCalculateResize(
                TerminalSafeHandle terminal,
                int width,
                int height,
                out TilSize dimensions);

            [DllImport(TerminalCore, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern void TerminalDpiChanged(
                TerminalSafeHandle terminal,
                int newDpi);

            //---------------------------------------------------------------------
            // Scrolling.
            //---------------------------------------------------------------------

            [DllImport(TerminalCore, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern void TerminalRegisterScrollCallback(
                TerminalSafeHandle terminal,
                [MarshalAs(UnmanagedType.FunctionPtr)] ScrollCallback callback);

            [DllImport(TerminalCore, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern void TerminalUserScroll(
                TerminalSafeHandle terminal,
                int viewTop);

            //---------------------------------------------------------------------
            // Selection.
            //---------------------------------------------------------------------

            [DllImport(TerminalCore, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern HRESULT TerminalStartSelection(
                TerminalSafeHandle terminal,
                TilPoint cursorPosition,
                bool altPressed);

            [DllImport(TerminalCore, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern HRESULT TerminalMoveSelection(
                TerminalSafeHandle terminal,
                TilPoint cursorPosition);

            [DllImport(TerminalCore, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern void TerminalClearSelection(
                TerminalSafeHandle terminal);

            [DllImport(TerminalCore, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.LPWStr)]
            public static extern string TerminalGetSelection(
                TerminalSafeHandle terminal);

            [DllImport(TerminalCore, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool TerminalIsSelectionActive(
                TerminalSafeHandle terminal);

            //---------------------------------------------------------------------
            // Appearance.
            //---------------------------------------------------------------------

            [DllImport(TerminalCore, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern void TerminalSetTheme(
                TerminalSafeHandle terminal,
                [MarshalAs(UnmanagedType.Struct)] TerminalTheme theme,
                string fontFamily,
                short fontSize,
                int newDpi);

            [DllImport(TerminalCore, CallingConvention = CallingConvention.StdCall)]
            public static extern void TerminalBlinkCursor(
                TerminalSafeHandle terminal);

            [DllImport(TerminalCore, CallingConvention = CallingConvention.StdCall)]
            public static extern void TerminalSetCursorVisible(
                TerminalSafeHandle terminal, bool visible);

            //---------------------------------------------------------------------
            // Focus.
            //---------------------------------------------------------------------

            [DllImport(TerminalCore, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern void TerminalSetFocus(
                TerminalSafeHandle terminal);

            [DllImport(TerminalCore, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern void TerminalKillFocus(
                TerminalSafeHandle terminal);

            [DllImport(User32, SetLastError = true)]
            public static extern IntPtr SetFocus(IntPtr hWnd);

            [DllImport(User32, SetLastError = true)]
            public static extern IntPtr GetFocus();

            [DllImport(User32, SetLastError = true)]
            public static extern short GetKeyState(int keyCode);

            [DllImport(User32, SetLastError = true)]
            public static extern uint GetCaretBlinkTime();
        }

        //---------------------------------------------------------------------
        // SafeHandles.
        //---------------------------------------------------------------------

        internal class TerminalSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            internal TerminalSafeHandle() : base(true)
            {
            }

            protected override bool ReleaseHandle()
            {
                if (this.handle != IntPtr.Zero)
                {
                    NativeMethods.DestroyTerminal(this.handle);
                }

                return true;
            }
        }

        internal class TerminalControl : Control
        {

        }
    }
}