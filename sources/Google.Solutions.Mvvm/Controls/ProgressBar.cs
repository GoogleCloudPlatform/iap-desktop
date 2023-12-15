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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    /// <summary>
    /// Base class for progress bars.
    /// </summary>
    public abstract class ProgressBarBase : Control
    {
        internal Timer? timer;
        private int value;
        private int maximum = 100;
        private int speed = 1;

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

        [Category("Behavior")]
        public int Speed
        {
            get => this.speed;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentException("Increment must be positive");
                }

                this.speed = Math.Min(this.Maximum, value);
            }
        }

        [Category("Behavior")]
        public bool Indeterminate
        {
            get => this.timer != null;
            set
            {
                if (value && this.timer == null)
                {
                    //
                    // Create a timer that advances the progress bar, but don't
                    // start it until the control is shown.
                    //
                    this.timer = new Timer()
                    {
                        Interval = 50,
                        Enabled = this.Visible && !this.DesignMode
                    };
                    this.timer.Tick += (_, __) =>
                    {
                        this.Value = (this.Value + this.Speed) % (this.Maximum + 1);
                    };
                }
                else if (!value && this.timer != null)
                {
                    this.timer.Stop();
                    this.timer.Dispose();
                    this.timer = null;
                }
            }
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                this.timer?.Stop();
                this.timer?.Dispose();
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (this.Indeterminate)
            {
                //
                // Only run the timer when the control is
                // visible.
                //
                Debug.Assert(this.timer != null);
                this.timer!.Enabled = this.Visible && !this.DesignMode;
            }
        }
    }

    /// <summary>
    /// Circular progress indicator/spinner.
    /// </summary>
    public class CircularProgressBar : ProgressBarBase
    {
        private int barWidth = 5;

        public CircularProgressBar()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            this.MinimumSize = new Size(3 * this.barWidth, 3 * this.barWidth);
            this.DoubleBuffered = true;
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
                        this.LineWidth,
                        this.LineWidth,
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
    /// Linear progress indicator/spinner.
    /// </summary>
    public class LinearProgressBar : ProgressBarBase
    {
        public LinearProgressBar()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.DoubleBuffered = true;
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using (var brush = new SolidBrush(this.ForeColor))
            {
                int start;
                int size;

                if (!this.Indeterminate)
                {
                    //
                    // Grow the bar to the right.
                    //
                    start = 0;
                    size = (int)Math.Ceiling(this.Width * ((float)this.Value / this.Maximum));
                }
                else
                {
                    var maxThirds = this.Maximum / 3;
                    if (this.Value < maxThirds)
                    {
                        //
                        // First third: Keep start fixed and grow the bar.
                        //
                        start = 0;
                        size = this.Width / 2 * this.Value / maxThirds;
                    }
                    else if (this.Value < maxThirds * 2)
                    {
                        //
                        // Second third: Move the bar while keeping it fixed in size.
                        //
                        start = this.Width / 2 * (this.Value - maxThirds) / maxThirds;
                        size = this.Width / 2;
                    }
                    else
                    {
                        //
                        // Last third: Shrink the bar.
                        //
                        start = this.Width / 2 + this.Width / 2 * (this.Value - 2 * maxThirds) / maxThirds;
                        size = this.Width - start;
                    }
                }

                var rect = new Rectangle()
                {
                    X = start,
                    Y = 0,
                    Width = size,
                    Height = this.Height
                };

                ButtonRenderer.DrawParentBackground(e.Graphics, this.ClientRectangle, this);
                e.Graphics.FillRectangle(brush, rect);
            }
        }
    }
}
