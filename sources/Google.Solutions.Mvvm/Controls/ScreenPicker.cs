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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    /// <summary>
    /// Control for picking screens, similar to the one used in
    /// the 'Display' control panel applet.
    /// </summary>
    [SkipCodeCoverage("View")]
    public partial class ScreenPicker<TModelItem> : UserControl
        where TModelItem : IScreenPickerModelItem
    {
        private Point currentMouseLocation = new Point(0, 0);

        private ObservableCollection<TModelItem>? model = null;

        public ScreenPicker()
        {
            InitializeComponent();

            this.ResizeRedraw = true;
            this.DoubleBuffered = true;
        }

        //---------------------------------------------------------------------
        // ScreenIcon.
        //---------------------------------------------------------------------

        internal class ScreenIcon
        {
            public TModelItem Model { get; }
            public Rectangle Bounds { get; }

            public ScreenIcon(
                TModelItem screen,
                Rectangle bounds)
            {
                this.Model = screen;
                this.Bounds = bounds;
            }
        }

        internal IEnumerable<ScreenIcon> Screens
        {
            get
            {
                if (this.model == null)
                {
                    return Enumerable.Empty<ScreenIcon>();
                }

                // Calculate a bounding box around all screens.
                var unionOfAllScreens = new Rectangle();
                foreach (var item in this.model)
                {
                    unionOfAllScreens = Rectangle.Union(unionOfAllScreens, item.ScreenBounds);
                }

                var scalingFactor = Math.Min(
                    (double)this.Width / unionOfAllScreens.Width,
                    (double)this.Height / unionOfAllScreens.Height);

                return this.model
                    .OrderBy(modelItem => modelItem.DeviceName)

                    // Shift bounds so that they have positive coordinates.
                    .Select(modelItem =>
                        new ScreenIcon(
                            modelItem,
                            new Rectangle(
                                modelItem.ScreenBounds.X + Math.Abs(unionOfAllScreens.X),
                                modelItem.ScreenBounds.Y + Math.Abs(unionOfAllScreens.Y),
                                modelItem.ScreenBounds.Width,
                                modelItem.ScreenBounds.Height)))

                    // Scale down to size of control.
                    .Select(icon =>
                        new ScreenIcon(
                            icon.Model,
                            new Rectangle(
                                (int)((double)icon.Bounds.X * scalingFactor),
                                (int)((double)icon.Bounds.Y * scalingFactor),
                                (int)((double)icon.Bounds.Width * scalingFactor),
                                (int)((double)icon.Bounds.Height * scalingFactor))))

                    // Add some padding
                    .Select(icon =>
                        new ScreenIcon(
                            icon.Model,
                            new Rectangle(
                                icon.Bounds.X + 2,
                                icon.Bounds.Y + 2,
                                icon.Bounds.Width - 4,
                                icon.Bounds.Height - 4)))
                    .ToList();
            }
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void ScreenSelector_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Color.Black, 2))
            {
                var screenOrdinal = 1;
                foreach (var screenIcon in this.Screens)
                {
                    e.Graphics.FillRectangle(
                        screenIcon.Model.IsSelected
                            ? SystemBrushes.Highlight
                            : SystemBrushes.ControlLight,
                        screenIcon.Bounds);
                    e.Graphics.DrawRectangle(
                        pen,
                        screenIcon.Bounds);
                    e.Graphics.DrawString(
                        (screenOrdinal++).ToString(),
                        this.Font,
                        screenIcon.Model.IsSelected
                            ? Brushes.White
                            : SystemBrushes.ControlText,
                        screenIcon.Bounds,
                        new StringFormat
                        {
                            LineAlignment = StringAlignment.Center,
                            Alignment = StringAlignment.Center
                        });
                }
            }
        }

        private void ScreenSelector_Click(object sender, EventArgs e)
        {
            var selected = this.Screens.FirstOrDefault(
                s => s.Bounds.Contains(this.currentMouseLocation));

            if (selected != null)
            {
                // Toggle selected state.
                selected.Model.IsSelected = !selected.Model.IsSelected;
                Invalidate();
            }
        }

        private void ScreenSelector_MouseMove(object sender, MouseEventArgs e)
        {
            this.currentMouseLocation = e.Location;
        }

        //---------------------------------------------------------------------
        // List Binding.
        //---------------------------------------------------------------------

        public void BindCollection(ObservableCollection<TModelItem> model)
        {
            this.model = model;
        }
    }

    public interface IScreenPickerModelItem
    {
        string DeviceName { get; }
        Rectangle ScreenBounds { get; }
        bool IsSelected { get; set; }
    }
}
