//
// Copyright 2024 Google LLC
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

using Google.Solutions.Mvvm.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Google.Solutions.Terminal.Controls
{
    /// <summary>
    /// Ancillary control used by ClientBase that indicates a client's
    /// connection state.
    /// </summary>
    internal class ClientStatePanel : UserControl
    {
        private ClientBase.ConnectionState state;
        private readonly Panel panel;
        private readonly Label stateLabel;
        private readonly LinearProgressBar progressBar;
        private readonly Button connectButton;

        public event EventHandler? ConnectButtonClicked;

        public ClientStatePanel()
        {
            SuspendLayout();

            this.panel = new Panel()
            {
                Width = 250,
                Height = 100,
            };
            this.Controls.Add(this.panel);

            this.stateLabel = new HeaderLabel()
            {
                AutoSize = false,
                Width = this.panel.Width,
                Height = 30,
                Location = new Point(0, 00),
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "..."
            };
            this.panel.Controls.Add(this.stateLabel);

            this.progressBar = new LinearProgressBar()
            {
                Width = this.panel.Width,
                Height = 5,
                Location = new Point(0, 40),
                Indeterminate = true,
                BackColor = Color.White,
                Visible = false
            };
            this.panel.Controls.Add(this.progressBar);

            this.connectButton = new Button()
            {
                Location = new Point(0, 60),
                Text = "Connect"
            };
            this.panel.Controls.Add(this.connectButton);
            this.connectButton.CenterHorizontally(this.panel);
            this.connectButton.Click += (sender, args) 
                => this.ConnectButtonClicked?.Invoke(sender, args);

            this.State = ClientBase.ConnectionState.NotConnected;

            ResumeLayout(false);
        }

        /// <summary>
        /// Update controls to reflect connection state.
        /// </summary>
        public ClientBase.ConnectionState State
        {
            get => this.state;
            set
            {
                this.state = value;

                switch (value)
                {
                    case ClientBase.ConnectionState.NotConnected:
                        this.stateLabel.Text = "Session disconnected";
                        this.progressBar.Visible = false;
                        this.connectButton.Visible = true;
                        break;

                    case ClientBase.ConnectionState.Connecting:
                        this.stateLabel.Text = "Connecting...";
                        this.progressBar.Visible = true;
                        this.connectButton.Visible = false;
                        break;

                    case ClientBase.ConnectionState.Disconnecting:
                        this.stateLabel.Text = "Disconnecting...";
                        this.progressBar.Visible = false;
                        this.connectButton.Visible = false;
                        break;

                    default:
                        this.progressBar.Visible = false;
                        break;
                }
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            this.panel.Location = new Point(
                (this.Width - this.panel.Width) / 2,
                (this.Height - this.panel.Height) * 4 / 5);
            base.OnSizeChanged(e);
        }
    }
}
