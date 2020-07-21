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
using Google.Solutions.Common.Util;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Services.Windows
{
    public interface IExceptionDialog
    {
        void Show(IWin32Window parent, string caption, Exception e);
    }

    /// <summary>
    /// Utility class for displaying exception information using 
    /// a "Vista style" dialog.
    /// </summary>
    public class ExceptionDialog : IExceptionDialog
    {
        private void ShowErrorDialog(IWin32Window parent, string caption, string message, string details)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(caption, message, details))
            {
                var config = new UnsafeNativeMethods.TASKDIALOGCONFIG()
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(UnsafeNativeMethods.TASKDIALOGCONFIG)),
                    hwndParent = parent.Handle,
                    dwFlags = 0,
                    dwCommonButtons = UnsafeNativeMethods.TASKDIALOG_COMMON_BUTTON_FLAGS.TDCBF_OK_BUTTON,
                    pszWindowTitle = "An error occured",
                    MainIcon = TaskDialogIcons.TD_ERROR_ICON,
                    pszMainInstruction = caption,
                    pszContent = message,
                    pszExpandedInformation = details.ToString()
                };

                UnsafeNativeMethods.TaskDialogIndirect(
                    ref config,
                    out int buttonPressed,
                    out int radioButtonPressed,
                    out bool verificationFlagPressed);
            }
        }

        public void Show(IWin32Window parent, string caption, Exception e)
        {
            e = e.Unwrap();

            using (TraceSources.IapDesktop.TraceMethod().WithParameters(caption, e))
            {
                if (e is GoogleApiException apiException && apiException.Error != null)
                {
                    // The .Message property contains a rather ugly concatenation of
                    // the information enclosed in the .Error object.

                    var details = new StringBuilder();
                    details.Append($"Status code: {apiException.Error.Code}\n\n");

                    foreach (var error in apiException.Error.Errors)
                    {
                        details.Append($"    Domain: {error.Domain}\n");
                        details.Append($"    Location: {error.Location}\n");
                        details.Append($"    Reason: {error.Reason}\n");
                        details.Append("\n");
                    }

                    TraceSources.IapDesktop.TraceError(
                        "Exception {0} ({1}): {2}",
                        e.GetType().Name,
                        caption,
                        details);

                    ShowErrorDialog(
                        parent,
                        caption,
                        apiException.Error.Message,
                        details.ToString());
                }
                else
                {
                    var details = new StringBuilder();

                    for (var innerException = e.InnerException;
                         innerException != null; innerException =
                         innerException.InnerException)
                    {
                        details.Append(e.InnerException.GetType().Name);
                        details.Append(":\n");
                        details.Append(innerException.Message);
                        details.Append("\n");
                    }

                    TraceSources.IapDesktop.TraceError(
                        "Exception {0} ({1}): {2}",
                        e.GetType().Name,
                        caption,
                        details);

                    ShowErrorDialog(
                        parent,
                        caption,
                        e.Message,
                        details.ToString());
                }
            }
        }
    }
}
