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
        private readonly Label infoLabel = new Label();

        protected override void OnCreateControl()
        {
            this.Dock = DockStyle.Fill;
            this.FixedPanel = FixedPanel.Panel1;
            this.IsSplitterFixed = true;
            this.Orientation = Orientation.Horizontal;
            this.SplitterWidth = 1;
            this.SplitterDistance = 25;
            this.Panel1.BackColor = this.NotificationBarBackColor;

            this.infoLabel.Location = new Point(30, 5);
            this.infoLabel.Size = new Size(this.Width - 40, this.SplitterDistance - 10);
            this.infoLabel.AutoEllipsis = true;
            this.infoLabel.AutoSize = false;
            this.infoLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.infoLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            this.infoLabel.ForeColor = SystemColors.InfoText;

            this.Panel1Collapsed = true;
            this.Panel1.Controls.Add(this.infoLabel);

            base.OnCreateControl();
        }

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
        public override string Text
        {
            get => this.infoLabel?.Text;
            set
            {
                this.infoLabel.Text = value;
                this.Panel1Collapsed = string.IsNullOrWhiteSpace(value);
            }
        }

        /// <summary>
        /// Gets or sets the background color of the bar.
        /// </summary>
        [Browsable(true)]
        [Category("Appearance")]
        public Color NotificationBarBackColor
        {
            get => this.Panel1.BackColor;
            set => this.Panel1.BackColor = value;
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
    }
}

