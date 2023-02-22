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

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    /// <summary>
    /// Circular progress indicator/spinner.
    /// </summary>
    public class CircularProgressBar : Control
    {
        private int value;
        private int maximum = 100;
        private int barWidth = 5;

        public CircularProgressBar()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Opaque, true);

            this.MinimumSize = new Size(10, 10);
            this.DoubleBuffered = true;
        }

        [Category("Behavior")]
        public int Value
        {
            get => this.value;
            set
            {
                this.value = Math.Min(this.maximum, value);
                Invalidate();
            }
        }

        [Category("Behavior")]
        public int Maximum
        {
            get => this.maximum;
            set
            {
                this.maximum = Math.Max(1, value);
                Invalidate();
            }
        }

        [Category("Appearance")]
        public int LineWidth
        {
            get => this.barWidth;
            set
            {
                this.barWidth = value;
                Invalidate();
            }
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            if (this.Width != this.Height)
            {
                //
                // Keep it rectangular.
                //
                this.Size = new Size()
                {
                    Width = Math.Min(this.Width, this.Height),
                    Height = Math.Min(this.Width, this.Height)
                };
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var bitmap = new Bitmap(this.Width, this.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                ButtonRenderer.DrawParentBackground(e.Graphics, this.ClientRectangle, this);

                using (var pen = new Pen(this.ForeColor, this.LineWidth)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                })
                {
                    var maxThirds = this.Maximum / 3;

                    float startAngle;
                    float sweepAngle;
                    if (this.Value < maxThirds)
                    {
                        //
                        // First third: Keep start fixed and grow the bar.
                        //
                        startAngle = 0.0f;
                        sweepAngle = 180.0f * this.Value / maxThirds;
                    }
                    else if (this.Value < 2 * maxThirds)
                    {
                        //
                        // Second third: Move the bar while keeping it fixed in size.
                        //
                        startAngle = 180.0f * (this.Value - maxThirds) / maxThirds;
                        sweepAngle = 180.0f;
                    }
                    else
                    {
                        //
                        // Last third: Shrink the bar.
                        //
                        startAngle = 180.0f + 180.0f * (this.Value - 2 * maxThirds) / maxThirds;
                        sweepAngle = 360.0f - startAngle;
                    }

                    graphics.DrawArc(
                        pen,
                        this.LineWidth, this.LineWidth,
                        (this.Width) - 2 * this.LineWidth,
                        (this.Height) - 2 * this.LineWidth,
                        startAngle - 90.0f, // Start at the top, not at the left.
                        sweepAngle);
                }

                e.Graphics.DrawImage(bitmap, 0, 0);
            }
        }
    }

    /// <summary>
    /// Circular progress bar that automatically advances indefinitely.
    /// </summary>
    public class IndeterminateCircularProgressBar : CircularProgressBar
    {
        private readonly Timer timer;
        private int increment = 3;

        public IndeterminateCircularProgressBar()
        {
            this.timer = new Timer()
            {
                Interval = 50
            };
            this.timer.Tick += (_, __) =>
            {
                this.Value = (this.Value + this.Increment) % this.Maximum;
            };
            this.timer.Start();
        }

        [Category("Behavior")]
        public int Increment
        {
            get => this.increment;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentException("Increment must be positive");
                }

                this.increment = Math.Min(this.Maximum, value);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                this.timer.Stop();
                this.timer.Dispose();
            }
        }
    }
}
