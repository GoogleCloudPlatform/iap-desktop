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

using Google.Solutions.Common.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Windows.Dialog
{
    public interface ITaskDialog
    {
        int ShowOptionsTaskDialog(
            IWin32Window parent,
            IntPtr mainIcon,
            string windowTitle,
            string mainInstruction,
            string content,
            string details,
            IList<string> optionCaptions,
            string verificationText,
            out bool verificationFlagPressed);
    }

    public static class TaskDialogIcons
    {
        public static readonly IntPtr TD_WARNING_ICON = new IntPtr(65535);
        public static readonly IntPtr TD_ERROR_ICON = new IntPtr(65534);
        public static readonly IntPtr TD_INFORMATION_ICON = new IntPtr(65533);
        public static readonly IntPtr TD_SHIELD_ICON = new IntPtr(65532);
        public static readonly IntPtr TD_SHIELD_ICON_GRAY_BACKGROUND = new IntPtr(65527);
        public static readonly IntPtr TD_SHIELD_ICON_GREEN_BACKGROUND = new IntPtr(65528);
        public static readonly IntPtr TD_SHIELD_ICON_INFO_BACKGROUND = new IntPtr(65531);
        public static readonly IntPtr TD_SHIELD_ICON_WARNING_BACKGROUND = new IntPtr(65530);
    }

    [SkipCodeCoverage("UI code")]
    public class TaskDialog : ITaskDialog
    {
        private readonly int ButtonIdOffset = 1000;

        public int ShowOptionsTaskDialog(
            IWin32Window parent,
            IntPtr mainIcon,
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
                Marshal.SizeOf<UnsafeNativeMethods.TASKDIALOG_BUTTON_RAW>() * options.Length);

            var currentButton = buttonsBuffer;
            for (var i = 0; i < options.Length; i++)
            {
                Marshal.StructureToPtr<UnsafeNativeMethods.TASKDIALOG_BUTTON_RAW>(
                    new UnsafeNativeMethods.TASKDIALOG_BUTTON_RAW()
                    {
                        nButtonID = this.ButtonIdOffset + i, // Add offset to avoid conflict with IDOK/IDCANCEL.
                        pszButtonText = options[i]
                    },
                    currentButton,
                    false);
                currentButton += Marshal.SizeOf<UnsafeNativeMethods.TASKDIALOG_BUTTON_RAW>();
            }

            try
            {
                var config = new UnsafeNativeMethods.TASKDIALOGCONFIG()
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(UnsafeNativeMethods.TASKDIALOGCONFIG)),
                    hwndParent = parent?.Handle ?? IntPtr.Zero,
                    dwFlags = UnsafeNativeMethods.TASKDIALOG_FLAGS.TDF_USE_COMMAND_LINKS,
                    dwCommonButtons = UnsafeNativeMethods.TASKDIALOG_COMMON_BUTTON_FLAGS.TDCBF_CANCEL_BUTTON,
                    pszWindowTitle = windowTitle,
                    MainIcon = mainIcon,
                    pszMainInstruction = mainInstruction,
                    pszContent = content,
                    pButtons = buttonsBuffer,
                    cButtons = (uint)options.Length,
                    pszExpandedInformation = details,
                    pszVerificationText = verificationText
                };

                UnsafeNativeMethods.TaskDialogIndirect(
                    ref config,
                    out var buttonPressed,
                    out var radioButtonPressed,
                    out verificationFlagPressed);

                if (buttonPressed >= this.ButtonIdOffset)
                {
                    // Option selected.
                    return buttonPressed - this.ButtonIdOffset;
                }
                else
                {
                    throw new OperationCanceledException("Task dialog was cancelled");
                }
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
