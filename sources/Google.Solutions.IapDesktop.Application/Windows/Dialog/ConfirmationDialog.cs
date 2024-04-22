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
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Windows.Dialog
{
    public interface IConfirmationDialog
    {
        DialogResult Confirm(
            IWin32Window? parent,
            string text,
            string caption,
            string title);
    }

    public class ConfirmationDialog : IConfirmationDialog
    {
        public DialogResult Confirm(
            IWin32Window? parent,
            string message,
            string caption,
            string title)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(caption, message))
            {
                var config = new UnsafeNativeMethods.TASKDIALOGCONFIG()
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(UnsafeNativeMethods.TASKDIALOGCONFIG)),
                    hwndParent = parent?.Handle ?? IntPtr.Zero,
                    dwFlags = 0,
                    dwCommonButtons =
                        UnsafeNativeMethods.TASKDIALOG_COMMON_BUTTON_FLAGS.TDCBF_YES_BUTTON |
                        UnsafeNativeMethods.TASKDIALOG_COMMON_BUTTON_FLAGS.TDCBF_NO_BUTTON,
                    pszWindowTitle = title,
                    pszMainInstruction = caption,
                    pszContent = message
                };

                UnsafeNativeMethods.TaskDialogIndirect(
                    ref config,
                    out var buttonPressed,
                    out _,
                    out _);

                return buttonPressed switch
                {
                    UnsafeNativeMethods.IDYES => DialogResult.Yes,
                    UnsafeNativeMethods.IDNO => DialogResult.No,
                    _ => DialogResult.Abort,
                };
            }
        }
    }
}
