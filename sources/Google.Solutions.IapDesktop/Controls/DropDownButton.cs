﻿//
// Copyright 2022 Google LLC
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

using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Controls
{
    /// <summary>
    /// Button with an optional drop-down menu.
    /// Based on <https://stackoverflow.com/a/27173509/4372>.
    /// </summary>
    public class DropDownButton : Button
    {
        [DefaultValue(null)]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public ContextMenuStrip Menu { get; set; }

        [DefaultValue(20)]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int SplitWidth { get; set; }

        public DropDownButton()
        {
            this.SplitWidth = 20;
        }

        protected override void OnMouseDown(MouseEventArgs args)
        {
            var splitRect = new Rectangle(
                this.Width - this.SplitWidth,
                0,
                this.SplitWidth,
                this.Height);

            if (this.Menu != null &&
                args.Button == MouseButtons.Left &&
                splitRect.Contains(args.Location))
            {
                //
                // Split arrow clicked.
                //
                this.Menu.Show(this, 0, this.Height);
            }
            else
            {
                //
                // Main button clicked.
                //
                base.OnMouseDown(args);
            }
        }

        protected override void OnPaint(PaintEventArgs args)
        {
            base.OnPaint(args);

            if (this.Menu != null && this.SplitWidth > 0)
            {
                //
                // Draw arrow.
                //
                int arrowX = this.ClientRectangle.Width - 14;
                int arrowY = this.ClientRectangle.Height / 2 - 1;

                var arrowBrush = this.Enabled
                    ? SystemBrushes.ControlText
                    : SystemBrushes.ButtonShadow;

                var arrowPoints = new[]
                {
                    new Point(arrowX, arrowY),
                    new Point(arrowX + 7, arrowY),
                    new Point(arrowX + 3, arrowY + 4)
                };

                args.Graphics.FillPolygon(arrowBrush, arrowPoints);

                //
                // Draw a dashed separator.
                //
                int lineX = this.ClientRectangle.Width - this.SplitWidth;
                int lineYFrom = arrowY - 4;
                int lineYTo = arrowY + 8;
                using (var separatorPen = new Pen(Brushes.DarkGray)
                {
                    DashStyle = DashStyle.Dot
                })
                {
                    args.Graphics.DrawLine(separatorPen, lineX, lineYFrom, lineX, lineYTo);
                }
            }
        }
    }
}
