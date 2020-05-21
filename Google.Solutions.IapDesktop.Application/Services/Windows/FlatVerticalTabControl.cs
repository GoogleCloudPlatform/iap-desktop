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

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Services.Windows
{
    internal class FlatVerticalTabControl : TabControl
    {
        public FlatVerticalTabControl()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw | ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer, true);
            SizeMode = TabSizeMode.Fixed;
            ItemSize = new Size(44, 136);
            Alignment = TabAlignment.Left;
            SelectedIndex = 0;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (Bitmap b = new Bitmap(Width, Height))
            using (Graphics g = Graphics.FromImage(b))
            {
                if (!DesignMode)
                {
                    SelectedTab.BackColor = SystemColors.Control;
                }

                g.Clear(SystemColors.Control);

                // Draw rectangle containing the tabs,
                g.FillRectangle(
                    new SolidBrush(Color.WhiteSmoke),
                    new Rectangle(0, 0, ItemSize.Height + 4, Height));


                var margin = 5;
                for (int i = 0; i <= TabCount - 1; i++)
                {
                    if (i == SelectedIndex)
                    {
                        var tabRect = new Rectangle(
                            new Point(GetTabRect(i).Location.X - 2 + margin, GetTabRect(i).Location.Y - 2 + margin),
                            new Size(GetTabRect(i).Width + 3 - 2 * margin, GetTabRect(i).Height - 1 - 2 * margin));

                        using (var highlightBrush = new SolidBrush(SystemColors.Highlight))
                        {
                            // Draw tab.
                            g.FillRectangle(highlightBrush, tabRect);
                            g.DrawRectangle(new Pen(Color.WhiteSmoke), tabRect);

                            // Draw the little arrow.
                            g.SmoothingMode = SmoothingMode.HighQuality;
                            Point[] arrowPoints =
                            {
                                new Point(ItemSize.Height + 3, GetTabRect(i).Location.Y + 20),
                                new Point(ItemSize.Height - 4, GetTabRect(i).Location.Y + 14),
                                new Point(ItemSize.Height - 4, GetTabRect(i).Location.Y + 27)
                            };

                            g.FillPolygon(highlightBrush, arrowPoints);
                            g.DrawPolygon(new Pen(SystemColors.Highlight), arrowPoints);

                            // Draw label.
                            g.DrawString(
                                TabPages[i].Text,
                                new Font(Font.FontFamily, Font.Size, FontStyle.Regular),
                                Brushes.White,
                                new Rectangle(
                                    tabRect.X + margin * 2,
                                    tabRect.Y,
                                    tabRect.Width - margin * 2,
                                    tabRect.Height),
                                new StringFormat
                                {
                                    LineAlignment = StringAlignment.Center,
                                    Alignment = StringAlignment.Near
                                });
                        }
                    }
                    else
                    {
                        var tabRect = new Rectangle(
                            new Point(GetTabRect(i).Location.X - 2, GetTabRect(i).Location.Y - 2),
                            new Size(GetTabRect(i).Width + 3, GetTabRect(i).Height - 1));

                        // Draw tab.
                        g.DrawString(
                            TabPages[i].Text,
                            Font,
                            Brushes.DimGray,
                            new Rectangle(
                                tabRect.X + margin * 3,
                                tabRect.Y,
                                tabRect.Width - margin * 3,
                                tabRect.Height),
                            new StringFormat
                            {
                                LineAlignment = StringAlignment.Center,
                                Alignment = StringAlignment.Near
                            });
                    }
                }

                e.Graphics.DrawImage(b, new Point(0, 0));
            }
        }
    }
}

