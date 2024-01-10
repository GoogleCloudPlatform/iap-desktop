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

using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    /// <summary>
    /// Parameters for a "Vista" style task dialog.
    /// </summary>
    public class TaskDialogParameters
    {
        /// <summary>
        /// Icon for the task dialog.
        /// </summary>
        public TaskDialogIcon? Icon { get; set; }

        /// <summary>
        /// Caption for title bar.
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        /// Main instruction.
        /// </summary>
        public string Heading { get; set; }

        /// <summary>
        /// Text content.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Footnote text.
        /// </summary>
        public string? Footnote { get; set; }

        /// <summary>
        /// Command buttons to show.
        /// </summary>
        public IList<TaskDialogButton> Buttons { get; } = new List<TaskDialogButton>();

        /// <summary>
        /// Verification text box in footer.
        /// </summary>
        public TaskDialogVerificationCheckBox? VerificationCheckBox { get; set; }

        public event EventHandler? LinkClicked;

        public TaskDialogParameters(string caption, string heading, string text)
        {
            this.Caption = caption;
            this.Heading = heading;
            this.Text = text;
        }

        internal void PerformLinkClick()
        {
            this.LinkClicked?.Invoke(this, EventArgs.Empty);
        }
    }

    public class TaskDialogVerificationCheckBox
    {
        public TaskDialogVerificationCheckBox(string text)
        {
            this.Text = text.ExpectNotEmpty(nameof(text));
        }

        /// <summary>
        /// Text to show next to checkbox.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Checkbox state.
        /// </summary>
        public bool Checked { get; set; }
    }

    public abstract class TaskDialogButton
    {
        protected TaskDialogButton(DialogResult result)
        {
            this.Result = result;
        }

        public DialogResult Result { get; }
    }

    /// <summary>
    /// Standard dialog button.
    /// </summary>
    public class TaskDialogStandardButton : TaskDialogButton
    {
        private const uint TDCBF_OK_BUTTON = 0x0001;
        private const uint TDCBF_YES_BUTTON = 0x0002;
        private const uint TDCBF_NO_BUTTON = 0x0004;
        private const uint TDCBF_CANCEL_BUTTON = 0x0008;
        private const uint TDCBF_RETRY_BUTTON = 0x0010;
        private const uint TDCBF_CLOSE_BUTTON = 0x0020;

        private const int IDOK = 1;
        private const int IDCANCEL = 2;
        private const int IDABORT = 3;
        private const int IDRETRY = 4;
        private const int IDIGNORE = 5;
        private const int IDYES = 6;
        private const int IDNO = 7;

        public static readonly TaskDialogStandardButton OK =
            new TaskDialogStandardButton(DialogResult.OK, IDOK, TDCBF_OK_BUTTON);

        public static readonly TaskDialogStandardButton Cancel
            = new TaskDialogStandardButton(DialogResult.Cancel, IDCANCEL, TDCBF_CANCEL_BUTTON);

        public static readonly TaskDialogStandardButton Yes =
            new TaskDialogStandardButton(DialogResult.Yes, IDYES, TDCBF_YES_BUTTON);

        public static readonly TaskDialogStandardButton No =
            new TaskDialogStandardButton(DialogResult.No, IDNO, TDCBF_NO_BUTTON);

        public static readonly TaskDialogStandardButton Retry =
            new TaskDialogStandardButton(DialogResult.Retry, IDRETRY, TDCBF_RETRY_BUTTON);

        public static readonly TaskDialogStandardButton Abort =
            new TaskDialogStandardButton(DialogResult.Abort, IDABORT, TDCBF_CLOSE_BUTTON);

        internal TaskDialogStandardButton(
            DialogResult result,
            int commandId,
            uint flag) : base(result)
        {
            this.CommandId = commandId;
            this.Flag = flag;
        }

        internal int CommandId { get; }

        internal uint Flag { get; }
    }

    /// <summary>
    /// Custom command link.
    /// </summary>
    public class TaskDialogCommandLinkButton : TaskDialogButton
    {
        public EventHandler? Click;

        public TaskDialogCommandLinkButton(
            string text,
            DialogResult result)
            : base(result)
        {
            this.Text = text.ExpectNotNull(nameof(text));
        }

        /// <summary>
        /// Command text.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Command text.
        /// </summary>
        public string? Details { get; set; }

        public void PerformClick()
        {
            this.Click?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Icon for the task dialog.
    /// </summary>
    public abstract class TaskDialogIcon : IDisposable
    {
        internal IntPtr Handle { get; }

        protected TaskDialogIcon(IntPtr handle)
        {
            this.Handle = handle;
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        //---------------------------------------------------------------------
        // Stock icons.
        //
        // These icons don't need disposal.
        //---------------------------------------------------------------------

        public static readonly TaskDialogIcon Warning = new StockIcon(65535);
        public static readonly TaskDialogIcon Error = new StockIcon(65534);
        public static readonly TaskDialogIcon Information = new StockIcon(65533);
        public static readonly TaskDialogIcon Shield = new StockIcon(65532);
        public static readonly TaskDialogIcon ShieldGrayBackground = new StockIcon(65527);
        public static readonly TaskDialogIcon ShieldGreenBackground = new StockIcon(65528);
        public static readonly TaskDialogIcon ShieldInfoBackground = new StockIcon(65531);
        public static readonly TaskDialogIcon ShieldWarningBackground = new StockIcon(65530);

        private class StockIcon : TaskDialogIcon
        {
            public StockIcon(int id) : base(new IntPtr(id))
            {
            }
        }
    }
}
