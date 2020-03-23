//
// Copyright 2019 Google LLC
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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Windows
{
    internal static class UnsafeNativeMethods
    {
        [Flags]
        internal enum TASKDIALOG_FLAGS : uint
        {
            TDF_ENABLE_HYPERLINKS = 0x0001,
            TDF_USE_HICON_MAIN = 0x0002,
            TDF_USE_HICON_FOOTER = 0x0004,
            TDF_ALLOW_DIALOG_CANCELLATION = 0x0008,
            TDF_USE_COMMAND_LINKS = 0x0010,
            TDF_USE_COMMAND_LINKS_NO_ICON = 0x0020,
            TDF_EXPAND_FOOTER_AREA = 0x0040,
            TDF_EXPANDED_BY_DEFAULT = 0x0080,
            TDF_VERIFICATION_FLAG_CHECKED = 0x0100,
            TDF_SHOW_PROGRESS_BAR = 0x0200,
            TDF_SHOW_MARQUEE_PROGRESS_BAR = 0x0400,
            TDF_CALLBACK_TIMER = 0x0800,
            TDF_POSITION_RELATIVE_TO_WINDOW = 0x1000,
            TDF_RTL_LAYOUT = 0x2000,
            TDF_NO_DEFAULT_RADIO_BUTTON = 0x4000,
            TDF_CAN_BE_MINIMIZED = 0x8000
        }

        [Flags]
        public enum TASKDIALOG_COMMON_BUTTON_FLAGS : uint
        {
            TDCBF_OK_BUTTON = 0x0001,
            TDCBF_YES_BUTTON = 0x0002,
            TDCBF_NO_BUTTON = 0x0004,
            TDCBF_CANCEL_BUTTON = 0x0008,
            TDCBF_RETRY_BUTTON = 0x0010,
            TDCBF_CLOSE_BUTTON = 0x0020,
        }

        public static readonly IntPtr TD_ERROR_ICON = new IntPtr(65534);
        public static readonly IntPtr TD_INFORMATION_ICON = new IntPtr(65533);
        public static readonly IntPtr TD_SHIELD_ICON = new IntPtr(65532);
        public static readonly IntPtr TD_SHIELD_ICON_INFO_BACKGROUND = new IntPtr(65531);
        public static readonly IntPtr TD_SHIELD_ICON_WARNING_BACKGROUND = new IntPtr(65530);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        internal struct TASKDIALOGCONFIG
        {
            public uint cbSize;
            public IntPtr hwndParent;
            public IntPtr hInstance;
            public TASKDIALOG_FLAGS dwFlags;
            public TASKDIALOG_COMMON_BUTTON_FLAGS dwCommonButtons;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszWindowTitle;

            public IntPtr MainIcon;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszMainInstruction;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszContent;

            public uint cButtons;

            public IntPtr pButtons;

            public int nDefaultButton;
            public uint cRadioButtons;
            public IntPtr pRadioButtons;
            public int nDefaultRadioButton;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszVerificationText;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszExpandedInformation;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszExpandedControlText;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszCollapsedControlText;

            public IntPtr FooterIcon;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszFooter;

            public TaskDialogCallback pfCallback;
            public IntPtr lpCallbackData;
            public uint cxWidth;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        internal struct TASKDIALOG_BUTTON_RAW
        {
            public int nButtonID;

            public IntPtr pszButtonText;
        }

        internal delegate int TaskDialogCallback([In] IntPtr hwnd, [In] uint msg, [In] UIntPtr wParam, [In] IntPtr lParam, [In] IntPtr refData);

        [DllImport("ComCtl32", CharSet = CharSet.Unicode, PreserveSig = false)]
        internal static extern void TaskDialogIndirect(
            [In] ref TASKDIALOGCONFIG pTaskConfig,
            [Out] out int pnButton,
            [Out] out int pnRadioButton,
            [Out] out bool pfVerificationFlagChecked);

        internal static int ShowOptionsTaskDialog(
            IWin32Window parent,
            string windowTitle,
            string mainInstruction,
            string content,
            string details,
            IList<string> optionCaptions,
            string verificationText,
            out bool verificationFlagPressed)
        {
            // The options to show.
            var options = optionCaptions
                .Select(caption => Marshal.StringToHGlobalUni(caption))
                .ToArray();

            // Wrap each option by a TASKDIALOG_BUTTON_RAW structure and 
            // marshal them one by one into a native memory buffer.
            var buttonsBuffer = Marshal.AllocHGlobal(
                Marshal.SizeOf<TASKDIALOG_BUTTON_RAW>() * options.Length);

            var currentButton = buttonsBuffer;
            for (int i = 0; i < options.Length; i++)
            {
                Marshal.StructureToPtr<TASKDIALOG_BUTTON_RAW>(
                    new TASKDIALOG_BUTTON_RAW()
                    {
                        nButtonID = i,
                        pszButtonText = options[i]
                    },
                    currentButton,
                    false);
                currentButton += Marshal.SizeOf<TASKDIALOG_BUTTON_RAW>();
            }

            try
            {
                var config = new TASKDIALOGCONFIG()
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(TASKDIALOGCONFIG)),
                    hwndParent = parent.Handle,
                    dwFlags = TASKDIALOG_FLAGS.TDF_USE_COMMAND_LINKS,
                    dwCommonButtons = TASKDIALOG_COMMON_BUTTON_FLAGS.TDCBF_OK_BUTTON |
                                      TASKDIALOG_COMMON_BUTTON_FLAGS.TDCBF_CANCEL_BUTTON,
                    pszWindowTitle = windowTitle,
                    MainIcon = TD_SHIELD_ICON_INFO_BACKGROUND,
                    pszMainInstruction = mainInstruction,
                    pszContent = content,
                    pButtons = buttonsBuffer,
                    cButtons = (uint)options.Length,
                    pszExpandedInformation = details,
                    pszVerificationText = verificationText
                };

                TaskDialogIndirect(
                    ref config,
                    out int buttonPressed,
                    out int radioButtonPressed,
                    out verificationFlagPressed);

                return buttonPressed;
            }
            finally
            {
                foreach (var option in options)
                {
                    Marshal.FreeHGlobal(option);
                }

                Marshal.FreeHGlobal(buttonsBuffer);
            }
        }
    }
}
