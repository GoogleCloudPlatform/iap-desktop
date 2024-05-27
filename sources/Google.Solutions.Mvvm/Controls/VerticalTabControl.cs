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

using Google.Solutions.Common.Diagnostics;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    /// <summary>
    /// A flat, vertical tab control similar to the one used in the Visual Studio 
    /// Project Designer.
    /// </summary>
    [SkipCodeCoverage("UI code")]
    public class VerticalTabControl : TabControl
    {
        private int textMargin = 10;

        public Color ActiveTabBackColor { get; set; } = SystemColors.Highlight;
        public Color ActiveTabForeColor { get; set; } = SystemColors.HighlightText;
        public Color InactiveTabBackColor { get; set; } = SystemColors.Control;
        public Color InactiveTabForeColor { get; set; } = SystemColors.ControlText;
        public Color HoverTabBackColor { get; set; } = SystemColors.ControlLight;
        public Color HoverTabForeColor { get; set; } = SystemColors.ControlText;
        public Color SheetBackColor { get; set; } = SystemColors.Control;

        public int TextMargin
        {
            get => this.textMargin;
            set
            {
                if (value < 0 || value > 20)
                {
                    throw new ArgumentException("The margin is invalid");
                }

                this.textMargin = value;
            }
        }

        public VerticalTabControl()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.ResizeRedraw
                    | ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer, true);
            this.SizeMode = TabSizeMode.Fixed;
            this.ItemSize = new Size(30, 140);
            this.Alignment = TabAlignment.Left;
            this.SelectedIndex = 0;
        }

        //---------------------------------------------------------------------
        // Paining.
        //---------------------------------------------------------------------

        //
        // NB. Because we're using a vertical layout,
        // ItemSize width and height are transposed.
        //
        protected int TabWidth => this.ItemSize.Height;

        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            base.ScaleControl(factor, specified);

            this.ItemSize = this.LogicalToDeviceUnits(this.ItemSize);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            Invalidate(new Rectangle(0, 0, this.TabWidth + 4, this.Height));
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            Invalidate(new Rectangle(0, 0, this.TabWidth + 4, this.Height));
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;

            using (var sheetBackgroundBrush = new SolidBrush(this.SheetBackColor))
            using (var activeBackgroundBrush = new SolidBrush(this.ActiveTabBackColor))
            using (var activeBackgroundPen = new Pen(this.ActiveTabBackColor))
            using (var activeTextBrush = new SolidBrush(this.ActiveTabForeColor))
            using (var inactiveBackgroundBrush = new SolidBrush(this.InactiveTabBackColor))
            using (var inactiveBackgroundPen = new Pen(this.InactiveTabBackColor))
            using (var inactiveTextBrush = new SolidBrush(this.InactiveTabForeColor))
            using (var hoverBackgroundBrush = new SolidBrush(this.HoverTabBackColor))
            using (var hoverBackgroundPen = new Pen(this.HoverTabBackColor))
            using (var hoverTextBrush = new SolidBrush(this.HoverTabForeColor))
            using (var centerNearFormat = new StringFormat
            {
                LineAlignment = StringAlignment.Center,
                Alignment = StringAlignment.Near
            })
            {
                //
                // Draw rectangle containing the tabs,
                //
                g.FillRectangle(
                    inactiveBackgroundBrush,
                    new Rectangle(0, 0, this.TabWidth + 4, this.Height));

                g.FillRectangle(
                    sheetBackgroundBrush,
                    new Rectangle(this.TabWidth + 4, 0, this.Width - this.TabWidth - 4, this.Height));

                for (var i = 0; i <= this.TabCount - 1; i++)
                {
                    //
                    // Use the tab rect, but leave some space at the left for the arrow
                    //
                    var boxRect = new Rectangle(
                        new Point(GetTabRect(i).Location.X, GetTabRect(i).Location.Y),
                        new Size(GetTabRect(i).Width - 6, GetTabRect(i).Height));
                    var textRect = new Rectangle(
                        boxRect.X + this.textMargin,
                        boxRect.Y,
                        boxRect.Width - this.textMargin,
                        boxRect.Height);

                    if (i == this.SelectedIndex)
                    {
                        //
                        // Draw active tab.
                        //

                        g.DrawRectangle(activeBackgroundPen, boxRect);
                        g.FillRectangle(activeBackgroundBrush, boxRect);

                        //
                        // Draw the little arrow.
                        //
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        Point[] arrowPoints =
                        {
                            new Point(this.TabWidth + 3, GetTabRect(i).Location.Y + boxRect.Height / 2), // 20),
                            new Point(this.TabWidth - 4, GetTabRect(i).Location.Y + boxRect.Height / 2 - 7), // 14),
                            new Point(this.TabWidth - 4, GetTabRect(i).Location.Y + boxRect.Height / 2 + 7), // 27)
                        };

                        g.FillPolygon(activeBackgroundBrush, arrowPoints);

                        g.DrawString(
                            this.TabPages[i].Text,
                            this.Font,
                            activeTextBrush,
                            textRect,
                            centerNearFormat);
                    }
                    else if (GetTabRect(i).Contains(PointToClient(Cursor.Position)))
                    {
                        //
                        // Draw mouseover tab.
                        //
                        g.DrawRectangle(hoverBackgroundPen, boxRect);
                        g.FillRectangle(hoverBackgroundBrush, boxRect);
                        g.DrawString(
                            this.TabPages[i].Text,
                            this.Font,
                            hoverTextBrush,
                            textRect,
                            centerNearFormat);
                    }
                    else
                    {
                        //
                        // Draw inactive tab.
                        //
                        g.DrawString(
                            this.TabPages[i].Text,
                            this.Font,
                            inactiveTextBrush,
                            textRect,
                            centerNearFormat);
                    }
                }
            }
        }
    }
}
