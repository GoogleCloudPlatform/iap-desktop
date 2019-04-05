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
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Plugin.Google.CloudIap.Gui
{
    /// <summary>
    /// Utility class for displaying exception information using 
    /// a "Vista style" dialog.
    /// </summary>
    public class ExceptionUtil
    {
        public static void HandleException(IWin32Window parent, string caption, Exception e)
        {
            if (e is AggregateException aggregate)
            {
                e = aggregate.InnerException;
            }

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

            var config = new UnsafeNativeMethods.TASKDIALOGCONFIG()
            {
                cbSize = (uint)Marshal.SizeOf(typeof(UnsafeNativeMethods.TASKDIALOGCONFIG)),
                hwndParent = parent.Handle,
                dwFlags = 0,
                dwCommonButtons = UnsafeNativeMethods.TASKDIALOG_COMMON_BUTTON_FLAGS.TDCBF_OK_BUTTON,
                pszWindowTitle = "An error occured",
                MainIcon = UnsafeNativeMethods.TD_ERROR_ICON,
                pszMainInstruction = caption,
                pszContent = e.Message,
                pszExpandedInformation = details.ToString()
            };

            UnsafeNativeMethods.TaskDialogIndirect(
                ref config,
                out int buttonPressed,
                out int radioButtonPressed,
                out bool verificationFlagPressed);
        }

    }
}
