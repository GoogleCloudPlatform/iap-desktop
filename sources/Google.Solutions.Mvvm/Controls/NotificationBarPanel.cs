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

using Google.Solutions.Mvvm.Shell;
using Google.Solutions.Mvvm.Controls;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    /// <summary>
    /// Panel that can show a notification bar at the top.
    /// </summary>
    public class NotificationBarPanel : SplitContainer
    {
        /// <summary>
        /// Height of bar, in logical units.
        /// </summary>
        private const int InfoNotificationBarHeight = 25;

        private readonly Label infoLabel = new Label();
        private readonly PictureBox icon = new PictureBox();

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override void OnCreateControl()
        {
            base.Dock = DockStyle.Fill;
            base.FixedPanel = FixedPanel.Panel1;
            base.IsSplitterFixed = true;
            base.Panel1.BackColor = SystemColors.Info;

            this.infoLabel.AutoEllipsis = true;
            this.infoLabel.AutoSize = false;
            this.infoLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.infoLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            this.infoLabel.ForeColor = SystemColors.InfoText;

            this.icon.Location = this.LogicalToDeviceUnits(new Point(5, (InfoNotificationBarHeight - 16) / 2));
            this.icon.Size = this.LogicalToDeviceUnits(new Size(16, 16));
            this.icon.Image = StockIcons.GetIcon(StockIcons.IconId.Info, StockIcons.IconSize.Small);

            base.Panel1Collapsed = true;
            base.Panel1.Controls.Add(this.infoLabel);
            base.Panel1.Controls.Add(this.icon);

            base.OnCreateControl();
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);

            if (this.Width > 0)
            {
                //
                // NB. During OnCreateControl, the size might still be (0, 0). Trying
                // to set Orientation in this state would cause an
                // InvalidOperationException. Therefore, we set the Orientation here.
                //
                base.SplitterWidth = 1;
                base.SplitterDistance = this.LogicalToDeviceUnits(InfoNotificationBarHeight);
                base.Orientation = Orientation.Horizontal;

                this.infoLabel.Size = new Size(
                    this.Width - this.LogicalToDeviceUnits(40), 
                    this.LogicalToDeviceUnits(InfoNotificationBarHeight - 10));
                this.infoLabel.Location = this.LogicalToDeviceUnits(new Point(30, 5));
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
        }

        //---------------------------------------------------------------------
        // Public properties.
        //---------------------------------------------------------------------

        /// <summary>
        /// Determine if the notification bar is currently visible.
        /// </summary>
        public bool NotificationBarVisible
            => !this.Panel1Collapsed;

        /// <summary>
        /// Gets or sets the text to be displayed in the notification bar. 
        /// If the text is null or empty, the bar is hidden.
        /// </summary>
        [Browsable(true)]
        [Category("Appearance")]
        public override string? Text
        {
            get => this.infoLabel?.Text;
            set
            {
                this.infoLabel.Text = value;
                base.Panel1Collapsed = string.IsNullOrWhiteSpace(value);
            }
        }

        /// <summary>
        /// Gets or sets the background color of the bar.
        /// </summary>
        [Browsable(true)]
        [Category("Appearance")]
        public Color NotificationBarBackColor
        {
            get => base.Panel1.BackColor;
            set => base.Panel1.BackColor = value;
        }

        /// <summary>
        /// Gets or sets the color of the notification text.
        /// </summary>
        [Browsable(true)]
        [Category("Appearance")]
        public Color NotificationBarForeColor
        {
            get => this.infoLabel.ForeColor;
            set => this.infoLabel.ForeColor = value;
        }

        //---------------------------------------------------------------------
        // Hiding properties.
        //---------------------------------------------------------------------

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new DockStyle Dock
        {
            get => base.Dock;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new bool IsSplitterFixed
        {
            get => base.IsSplitterFixed;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new bool Panel1Collapsed
        {
            get => base.Panel1Collapsed;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new bool Panel2Collapsed
        {
            get => base.Panel2Collapsed;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new int Panel1MinSize
        {
            get => base.Panel1MinSize;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new int SplitterDistance
        {
            get => base.SplitterDistance;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new int SplitterWidth
        {
            get => base.SplitterWidth;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Orientation Orientation
        {
            get => base.Orientation;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new SplitterPanel Panel1
        {
            get => base.Panel1;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Point Location
        {
            get => base.Location;
        }
    }
}

