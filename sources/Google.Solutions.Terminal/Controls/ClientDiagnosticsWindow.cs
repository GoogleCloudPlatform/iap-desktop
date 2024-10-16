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

using Google.Solutions.Common.Util;
using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Google.Solutions.Terminal.Controls
{
    /// <summary>
    /// Simple window that shows a client and its properties.
    /// 
    /// For testing only.
    /// </summary>
    public class ClientDiagnosticsWindow<TClient> : Form
        where TClient : ClientBase
    {
        public TClient Client { get; }

        public ToolStripMenuItem ClientMenu { get; }

        private void ShowError(string caption, Exception e)
        {
            MessageBox.Show(
                this,
                e.FullMessage(),
                caption,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        public ClientDiagnosticsWindow(TClient client)
        {
            this.Client = client;

            SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(96, 96);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1400, 1000);
            this.Text = typeof(TClient).Name;

            //
            // Split container.
            //
            var splitContainer = new SplitContainer()
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 450
            };
            splitContainer.BeginInit();
            this.Controls.Add(splitContainer);

            //
            // Client.
            //
            client.Dock = DockStyle.Fill;
            splitContainer.Panel1.Controls.Add(client);

            //
            // PropertyGrid.
            //
            var propertyGrid = new PropertyGrid()
            {
                Dock = DockStyle.Fill,
                SelectedObject = client,
            };
            splitContainer.Panel2.Controls.Add(propertyGrid);

            //
            // Main menu.
            //
            var menu = new MenuStrip();
            this.MainMenuStrip = menu;
            this.Controls.Add(menu);

            this.ClientMenu = new ToolStripMenuItem("&Client");
            menu.Items.Add(this.ClientMenu);

            //
            // Add menu item for each public parameter-less method.
            //
            foreach (var method in client.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => typeof(ClientBase).IsAssignableFrom(m.DeclaringType))
                .Where(m => !m.IsSpecialName)
                .Where(m => !m.GetParameters().Any()))

            {
                this.ClientMenu.DropDownItems.Add(method.Name).Click += (_, __) =>
                {
                    try
                    {
                        method.Invoke(client, null);
                    }
                    catch (Exception e)
                    {
                        ShowError("The command failed", e);
                    }
                };
            }

            splitContainer.EndInit();
            ResumeLayout(false);

            client.ConnectionFailed += (_, args)
                => ShowError("Connection failed", args.Exception);
        }
    }
}
