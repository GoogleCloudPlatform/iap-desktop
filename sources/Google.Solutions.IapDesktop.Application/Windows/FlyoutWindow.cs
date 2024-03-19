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

using Google.Solutions.Common.Diagnostics;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Windows
{
    [SkipCodeCoverage("View")]
    public partial class FlyoutWindow : Form
    {
        public Color BorderColor { get; set; } = SystemColors.ActiveBorder;

        public IWin32Window? FlyoutOwner { get; private set; }

        protected FlyoutWindow()
        {
            InitializeComponent();
        }

        public void Show(
            IWin32Window owner,
            Rectangle screenPositionOfControlToAlignTo,
            ContentAlignment alignment)
        {
            if (this.FlyoutOwner != null)
            {
                throw new InvalidOperationException("Window has already been shown");
            }

            this.FlyoutOwner = owner;

            //
            // Position the window relative to the reference control. 
            //
            // TopLeft:
            //
            //    +---------------+
            //    | Flyout window |
            //    +-----+---------+
            //          | Control |
            //          +---------+
            //
            // TopRight:
            //
            //          +---------------+
            //          | Flyout window |
            //          +---------+-----+
            //          | Control |
            //          +---------+
            //

            int offsetX, offsetY;

            switch (alignment)
            {
                case ContentAlignment.TopRight:
                    offsetX = 0;
                    offsetY = -this.Height;
                    break;

                case ContentAlignment.BottomRight:
                    offsetX = 0;
                    offsetY = screenPositionOfControlToAlignTo.Height;
                    break;

                case ContentAlignment.TopLeft:
                    offsetX = screenPositionOfControlToAlignTo.Width - this.Width;
                    offsetY = -this.Height;
                    break;

                case ContentAlignment.BottomLeft:
                    offsetX = screenPositionOfControlToAlignTo.Width - this.Width;
                    offsetY = screenPositionOfControlToAlignTo.Height;
                    break;

                default:
                    throw new ArgumentException(nameof(alignment));
            }

            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(
                screenPositionOfControlToAlignTo.Location.X + offsetX,
                screenPositionOfControlToAlignTo.Location.Y + offsetY);

            Show(owner);
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void FlyoutWindow_Deactivate(object sender, EventArgs e)
        {
            //
            // Window lost focus -> automatically close to imitate a tooltip
            // behavior.
            //
            Close();
        }

        private void FlyoutWindow_Paint(object sender, PaintEventArgs e)
        {
            // Draw border around form.
            ControlPaint.DrawBorder(
                e.Graphics,
                this.ClientRectangle,
                this.BorderColor,
                ButtonBorderStyle.Solid);
        }
    }
}
