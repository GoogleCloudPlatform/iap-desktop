//
// Copyright 2020 Google LLC
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

using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Interop;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Windows
{
    public class DocumentWindow : ToolWindowViewBase
    {
        /// <summary>
        /// Hotkey to move focus to current document, or release focus
        /// back to main window.
        /// </summary>
        public const Keys ToggleFocusHotKey = Keys.Control | Keys.Alt | Keys.Home;

        protected IMainWindow MainWindow { get; }

        /// <summary>
        /// Size of window when it was not floating.
        /// </summary>
        protected Size? PreviousNonFloatingSize { get; private set; }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

#pragma warning disable CS8618 // Uninitialzed, non-nullable fields
        protected DocumentWindow()
        {
            Debug.Assert(
                this.DesignMode,
                "Constructor is for designer only");
        }
#pragma warning restore CS8618

        public DocumentWindow(
            IServiceProvider serviceProvider)
            : base(serviceProvider, DockState.Document)
        {
            this.MainWindow = serviceProvider.GetService<IMainWindow>();

            this.DockAreas = DockAreas.Document | DockAreas.Float;

            this.SizeChanged += (sender, args) =>
            {
                if (this.Pane?.FloatWindow == null)
                {
                    //
                    // Keep track of size for as long as the window
                    // isn't floating.
                    //
                    this.PreviousNonFloatingSize = this.Size;
                }
                else if (this.PreviousNonFloatingSize != null &&
                    this.Size != this.PreviousNonFloatingSize.Value &&
                    Math.Abs(this.Size.Width - this.PreviousNonFloatingSize.Value.Width) <= 2 &&
                    Math.Abs(this.Size.Height - this.PreviousNonFloatingSize.Value.Height) <= 2)
                {
                    //
                    // The floating size is really close to the size the window had when
                    // it was non-floating. This discrepancy is most likely caused by the
                    // docking library and is unintentional (we try to size the window
                    // so that it fits the required client size, but that doesn't always
                    // match exactly).
                    //
                    // Adjust the size so that it fits the previous size. That way,
                    // we avoid having to resize the contents (which can be expensive).
                    //
                    this.Size = this.PreviousNonFloatingSize.Value;
                }
            };
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        protected void ShowTooltip(string title, string text)
        {
            var toolTip = new ToolTip()
            {
                AutoPopDelay = 2000,
                InitialDelay = 0,
                IsBalloon = true,
                ShowAlways = true,
                ToolTipTitle = title
            };

            toolTip.Show(string.Empty, this, 0);
            toolTip.Show(
                text,
                this,
                new Point(this.Width / 10, this.Height / 10),
                4000);
        }

        //---------------------------------------------------------------------
        // Drag/docking support.
        //---------------------------------------------------------------------

        private bool closeMessageReceived = false;

        protected override void WndProc(ref Message m)
        {
            if (!this.DesignMode)
            {
                switch (m.Id())
                {
                    case WindowMessage.WM_CLOSE:
                        this.closeMessageReceived = true;
                        break;

                    case WindowMessage.WM_DESTROY:
                        if (!this.closeMessageReceived)
                        {
                            //
                            // A WM_DESTROY that's not preceeded by a WM_CLOSE
                            // indicates that the window is being re-docked.
                            //
                            OnDockBegin();
                        }

                        break;

                    case WindowMessage.WM_SHOWWINDOW:
                        OnDockEnd();
                        break;
                }
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// Client size that a float window should default to.
        /// </summary>
        protected virtual Size DefaultFloatWindowClientSize
        {
            //
            // Try to size the floating window so that it matches its previous
            // non-floating size.
            //
            get => this.PreviousNonFloatingSize ?? this.Size;
        }

        protected virtual void OnDockBegin()
        { }

        protected virtual void OnDockEnd()
        { }

        /// <summary>
        /// Switch focus to this document.
        /// </summary>
        public void SwitchToDocument()
        {
            base.ShowWindow();
        }
    }
}
