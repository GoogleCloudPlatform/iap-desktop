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

using Google.Solutions.Apis.Diagnostics;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Windows.Dialog
{
    public interface IExceptionDialog
    {
        void Show(
            IWin32Window? parent,
            string caption,
            Exception e);
    }

    /// <summary>
    /// Utility class for displaying exception information using 
    /// a "Vista style" dialog.
    /// </summary>
    public class ExceptionDialog : IExceptionDialog
    {
        private readonly HelpClient helpAdapter;
        private readonly BugReportClient buganizerAdapter;

        public ExceptionDialog(
            HelpClient helpAdapter,
            BugReportClient buganizerAdapter)
        {
            this.helpAdapter = helpAdapter.ExpectNotNull(nameof(helpAdapter));
            this.buganizerAdapter = buganizerAdapter.ExpectNotNull(nameof(buganizerAdapter));
        }

        private static bool ShouldShowBugReportLink(Exception e)
        {
            //
            // Avoid showing a link for all exceptions, as many
            // exceptions are benign. But any unwrapped System.*
            // exceptions are likely to indicate some sort of bug.
            //
            return e.GetType().Namespace.StartsWith("System");
        }

        private void ShowErrorDialogWithHelp(
            IWin32Window? parent,
            string caption,
            string message,
            string details,
            IHelpTopic? helpTopic,
            BugReport? bugReport)
        {
            Debug.Assert(!(parent is Control control) || !control.InvokeRequired);

            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(caption, message, details))
            {
                var config = new UnsafeNativeMethods.TASKDIALOGCONFIG()
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(UnsafeNativeMethods.TASKDIALOGCONFIG)),
                    hwndParent = parent?.Handle ?? IntPtr.Zero,
                    dwFlags = 0,
                    dwCommonButtons = UnsafeNativeMethods.TASKDIALOG_COMMON_BUTTON_FLAGS.TDCBF_OK_BUTTON,
                    pszWindowTitle = "An error occurred",
                    MainIcon = TaskDialogIcons.TD_ERROR_ICON,
                    pszMainInstruction = caption,
                    pszContent = message,
                    pszExpandedInformation = details.ToString()
                };

                if (helpTopic != null)
                {
                    //
                    // Add help link to footer.
                    //
                    config.FooterIcon = TaskDialogIcons.TD_INFORMATION_ICON;
                    config.dwFlags |= UnsafeNativeMethods.TASKDIALOG_FLAGS.TDF_EXPAND_FOOTER_AREA |
                                      UnsafeNativeMethods.TASKDIALOG_FLAGS.TDF_ENABLE_HYPERLINKS;
                    config.pszFooter = $"For more information, see <A HREF=\"#\">{helpTopic.Title}</A>";
                    config.pfCallback = (hwnd, notification, wParam, lParam, refData) =>
                    {
                        if (notification == UnsafeNativeMethods.TASKDIALOG_NOTIFICATIONS.TDN_HYPERLINK_CLICKED)
                        {
                            this.helpAdapter.OpenTopic(helpTopic);
                        }

                        return 0; // S_OK;
                    };
                }
                else if (bugReport != null)
                {
                    //
                    // Add bug report link to footer.
                    //
                    config.FooterIcon = TaskDialogIcons.TD_INFORMATION_ICON;
                    config.dwFlags |= UnsafeNativeMethods.TASKDIALOG_FLAGS.TDF_EXPAND_FOOTER_AREA |
                                      UnsafeNativeMethods.TASKDIALOG_FLAGS.TDF_ENABLE_HYPERLINKS;
                    config.pszFooter = "If this looks wrong, consider <A HREF=\"#\">reporting an issue</A>.";
                    config.pfCallback = (hwnd, notification, wParam, lParam, refData) =>
                    {
                        if (notification == UnsafeNativeMethods.TASKDIALOG_NOTIFICATIONS.TDN_HYPERLINK_CLICKED)
                        {
                            this.buganizerAdapter.ReportBug(bugReport);
                        }

                        return 0; // S_OK;
                    };
                }

                try
                {
                    UnsafeNativeMethods.TaskDialogIndirect(
                        ref config,
                        out var buttonPressed,
                        out var radioButtonPressed,
                        out var verificationFlagPressed);
                }
                catch (COMException e)
                {
                    ApplicationTraceSource.Log.TraceError(e);

                    //
                    // TaskDialogIndirect can fail spuriously, typically with a 
                    // 0x80070715 error (The specified resource type cannot be
                    // found in the image file). Cf. b/244620235, b/239776121.
                    //
                    // Fall back to a classic MessageBox.
                    //
                    MessageBox.Show(
                        parent,
                        message,
                        caption,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        public void Show(
            IWin32Window? parent,
            string caption,
            Exception e)
        {
            Debug.Assert(!(parent is Control control) || !control.InvokeRequired);

            e = e.Unwrap();

            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(caption, e))
            {
                var details = new StringBuilder();
                var message = string.Empty;
                if (e is GoogleApiException apiException && apiException.Error != null)
                {
                    // The .Message property contains a rather ugly concatenation of
                    // the information enclosed in the .Error object.

                    details.Append($"Status code: {apiException.Error.Code}\n\n");

                    foreach (var error in apiException.Error.Errors.EnsureNotNull())
                    {
                        details.Append($"    Domain: {error.Domain}\n");
                        details.Append($"    Location: {error.Location}\n");
                        details.Append($"    Reason: {error.Reason}\n");
                        details.Append("\n");
                    }

                    message = apiException.Error.Message;
                }
                else
                {
                    for (var innerException = e.InnerException;
                         innerException != null; innerException =
                         innerException.InnerException)
                    {
                        details.Append(e.InnerException.GetType().Name);
                        details.Append(":\n");
                        details.Append(innerException.Message);
                        details.Append("\n");
                    }

                    message = e.Message;
                }

                ApplicationTraceSource.Log.TraceError(
                    "Exception {0} ({1}): {2}",
                    e.GetType().Name,
                    caption,
                    details);

                var bugReport = ShouldShowBugReportLink(e)
                    ? new BugReport(GetType(), e)
                    {
                        SourceWindow = parent
                    }
                    : null;

                ShowErrorDialogWithHelp(
                    parent,
                    caption,
                    message,
                    details.ToString(),
                    (e as IExceptionWithHelpTopic)?.Help,
                    bugReport);
            }
        }
    }
}
