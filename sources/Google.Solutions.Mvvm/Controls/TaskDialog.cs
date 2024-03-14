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
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    /// <summary>
    /// A "Vista" style task dialog.
    /// </summary>
    public interface ITaskDialog
    {
        /// <summary>
        /// Show a dialog.
        /// </summary>
        DialogResult ShowDialog(
            IWin32Window parent,
            TaskDialogParameters parameters);
    }

    public class TaskDialog : ITaskDialog
    {
        internal const int CommandLinkIdOffset = 1000;

        /// <summary>
        /// Surrogate function, for testing only.
        /// </summary>
        internal NativeMethods.TaskDialogIndirectDelegate? TaskDialogIndirect { get; set; }

        public DialogResult ShowDialog(
            IWin32Window parent,
            TaskDialogParameters parameters)
        {
            parameters.ExpectNotNull(nameof(parameters));

            if (!parameters.Buttons.Any())
            {
                throw new InvalidOperationException
                    ("The dialog must contain at least one button");
            }

            var standardButtons = parameters.Buttons
                .OfType<TaskDialogStandardButton>()
                .ToList();
            var commandButtons = parameters.Buttons
                .OfType<TaskDialogCommandLinkButton>()
                .ToList();

            using (var commandButtonsHandle = LocalAllocSafeHandle.LocalAlloc(
                (uint)(Marshal.SizeOf<TASKDIALOG_BUTTON_RAW>() * commandButtons.Count)))
            {
                //
                // Prepare native struct for command buttons.
                //
                var commandButtonTexts = commandButtons
                    .Select(b =>
                    {
                        //
                        // Text up to the first new line character is treated as the
                        // command link's main text, the remainder is treated as the
                        // command link's note. 
                        //
                        var text = b.Text.Replace('\n', ' ');

                        if (b.Details != null)
                        {
                            text += $"\n{b.Details}";
                        }

                        return text;
                    })
                    .Select(text => Marshal.StringToHGlobalUni(text))
                    .ToArray();

                for (var i = 0; i < commandButtons.Count; i++)
                {
                    Marshal.StructureToPtr(
                        new TASKDIALOG_BUTTON_RAW()
                        {
                            //
                            // Add ID offset to avoid conflict with IDOK/IDCANCEL.
                            //
                            nButtonID = CommandLinkIdOffset + i,
                            pszButtonText = commandButtonTexts[i]
                        },
                        commandButtonsHandle.DangerousGetHandle() + i * Marshal.SizeOf<TASKDIALOG_BUTTON_RAW>(),
                        false);
                }

                try
                {
                    var flags =
                        TASKDIALOG_FLAGS.TDF_EXPAND_FOOTER_AREA |
                        TASKDIALOG_FLAGS.TDF_ENABLE_HYPERLINKS;
                    if (commandButtons.Any())
                    {
                        flags |= TASKDIALOG_FLAGS.TDF_USE_COMMAND_LINKS;
                    }

                    var config = new TASKDIALOGCONFIG()
                    {
                        cbSize = (uint)Marshal.SizeOf<TASKDIALOGCONFIG>(),
                        hwndParent = parent?.Handle ?? IntPtr.Zero,
                        dwFlags = flags,
                        dwCommonButtons = standardButtons
                            .Select(b => b.Flag)
                            .Aggregate((f1, f2) => f1 | f2),
                        pszWindowTitle = parameters.Caption,
                        MainIcon = parameters.Icon?.Handle ?? IntPtr.Zero,
                        pszMainInstruction = parameters.Heading,
                        pszContent = parameters.Text,
                        pButtons = commandButtons.Any() ? commandButtonsHandle.DangerousGetHandle() : IntPtr.Zero,
                        cButtons = (uint)commandButtons.Count,
                        pszExpandedInformation = parameters.Footnote,
                        pszVerificationText = parameters.VerificationCheckBox?.Text,
                        pfCallback = (hwnd, notification, wParam, lParam, refData) =>
                        {
                            if (notification == TASKDIALOG_NOTIFICATIONS.TDN_HYPERLINK_CLICKED)
                            {
                                parameters.PerformLinkClick();
                            }

                            return HRESULT.S_OK;
                        }
                    };

                    var function = this.TaskDialogIndirect ?? NativeMethods.TaskDialogIndirect;
                    function(
                        ref config,
                        out var buttonIdPressed,
                        out var _,
                        out var verificationFlagPressed);

                    if (parameters.VerificationCheckBox != null)
                    {
                        parameters.VerificationCheckBox.Checked = verificationFlagPressed;
                    }

                    //
                    // Map the result back to the right button.
                    //
                    if (buttonIdPressed >= CommandLinkIdOffset &&
                        buttonIdPressed < CommandLinkIdOffset + commandButtons.Count)
                    {
                        var pressedCommandButton = commandButtons[buttonIdPressed - CommandLinkIdOffset];
                        pressedCommandButton.PerformClick();
                        return pressedCommandButton.Result;
                    }
                    else if (standardButtons.FirstOrDefault(b => b.CommandId == buttonIdPressed)
                        is var pressedStandardButton &&
                        pressedStandardButton != null)
                    {
                        return pressedStandardButton.Result;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"The TaskDialog returned an unexpected result: {buttonIdPressed}");
                    }
                }
                finally
                {
                    foreach (var commandButtonText in commandButtonTexts)
                    {
                        Marshal.FreeHGlobal(commandButtonText);
                    }
                }
            }
        }

        //---------------------------------------------------------------------
        // P/Invoke.
        //---------------------------------------------------------------------

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
        internal enum TASKDIALOG_COMMON_BUTTON_FLAGS : uint
        {
            TDCBF_OK_BUTTON = 0x0001,
            TDCBF_YES_BUTTON = 0x0002,
            TDCBF_NO_BUTTON = 0x0004,
            TDCBF_CANCEL_BUTTON = 0x0008,
            TDCBF_RETRY_BUTTON = 0x0010,
            TDCBF_CLOSE_BUTTON = 0x0020,
        }

        internal enum TASKDIALOG_NOTIFICATIONS : uint
        {
            TDN_CREATED = 0,
            TDN_NAVIGATED = 1,
            TDN_BUTTON_CLICKED = 2,
            TDN_HYPERLINK_CLICKED = 3,
            TDN_TIMER = 4,
            TDN_DESTROYED = 5,
            TDN_RADIO_BUTTON_CLICKED = 6,
            TDN_DIALOG_CONSTRUCTED = 7,
            TDN_VERIFICATION_CLICKED = 8,
            TDN_HELP = 9,
            TDN_EXPANDO_BUTTON_CLICKED = 10
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        internal struct TASKDIALOGCONFIG
        {
            public uint cbSize;
            public IntPtr hwndParent;
            public IntPtr hInstance;
            public TASKDIALOG_FLAGS dwFlags;
            public uint dwCommonButtons;

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
            public string? pszVerificationText;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string? pszExpandedInformation;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszExpandedControlText;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszCollapsedControlText;

            public IntPtr FooterIcon;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszFooter;

            public NativeMethods.TaskDialogCallback pfCallback;
            public IntPtr lpCallbackData;
            public uint cxWidth;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        internal struct TASKDIALOG_BUTTON_RAW
        {
            public int nButtonID;

            public IntPtr pszButtonText;
        }


        internal static class NativeMethods
        {
            internal delegate HRESULT TaskDialogCallback(
                [In] IntPtr hwnd,
                [In] TASKDIALOG_NOTIFICATIONS msg,
                [In] UIntPtr wParam,
                [In] IntPtr lParam,
                [In] IntPtr refData);

            internal delegate void TaskDialogIndirectDelegate(
                [In] ref TASKDIALOGCONFIG pTaskConfig,
                [Out] out int pnButton,
                [Out] out int pnRadioButton,
                [Out] out bool pfVerificationFlagChecked);

            [DllImport("ComCtl32", CharSet = CharSet.Unicode, PreserveSig = false)]
            internal static extern void TaskDialogIndirect(
                [In] ref TASKDIALOGCONFIG pTaskConfig,
                [Out] out int pnButton,
                [Out] out int pnRadioButton,
                [Out] out bool pfVerificationFlagChecked);
        }
    }
}
