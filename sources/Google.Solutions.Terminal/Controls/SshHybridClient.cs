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
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Terminal.Controls
{
    /// <summary>
    /// Client that connects a virtual terminal to an SSH shell channel,
    /// and optionally allow browsing the remote file system using SFTP.
    /// </summary>
    public class SshHybridClient : SshShellClient
    {
        private readonly SplitContainer container;
        private readonly SplitterPanel terminalPanel;
        private readonly SplitterPanel fileBrowserPanel;
        private IBindingContext? bindingContext;
        private FileBrowser? fileBrowser;

        public SshHybridClient()
        {
            SuspendLayout();

            this.container = new SplitContainer()
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterIncrement = 10,
                SplitterWidth = 6,
            };
            this.Controls.Add(this.container);

            this.terminalPanel = this.container.Panel1;
            this.fileBrowserPanel = this.container.Panel2;

            //
            // Move terminal into panel1.
            //
            this.Controls.Remove(this.Terminal);
            this.terminalPanel.Controls.Add(this.Terminal);

            //
            // Only show terminal by default.
            //
            this.container.Panel1Collapsed = true;
            this.container.Panel2Collapsed = true;

            this.container.Panel2MinSize = 10;
            this.container.SplitterMoved += OnSplitterMoved;

            ResumeLayout(false);
        }

        public bool CanShowFileBrowser
        {
            //
            // The browser can only be shown in logged-on state.
            //
            get => 
                this.State == ConnectionState.LoggedOn && 
                this.BindingContext != null;
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override void OnStateChanged()
        {
            base.OnStateChanged();

            if (!this.CanShowFileBrowser)
            {
                //
                // Hide file browser as soon as we're not in logged-on state
                // anymore.
                //
                this.IsFileBrowserVisible = false;
            }
        }

        //---------------------------------------------------------------------
        // Splitter events.
        //---------------------------------------------------------------------

        private void OnSplitterMoved(object sender, SplitterEventArgs e)
        {
            if (this.fileBrowserPanel.Height <= this.container.Panel2MinSize * 2)
            {
                //
                // File browser panel resized to almost minimum size, hide entirely.
                //
                this.IsFileBrowserVisible = false;
            }
        }

        //---------------------------------------------------------------------
        // File browser.
        //---------------------------------------------------------------------

        /// <summary>
        /// Enable binding.
        /// </summary>
        public override void Bind(IBindingContext bindingContext)
        {
            this.bindingContext = bindingContext;
        }

        /// <summary>
        /// Toggle visibility of SFTP file browser.
        /// </summary>
        public bool IsFileBrowserVisible
        {
            get => !this.container.Panel2Collapsed;
            set
            {
                if (value == this.IsFileBrowserVisible)
                {
                    //
                    // No change.
                    //
                    return;
                }
                else if (value)
                {
                    Debug.Assert(this.fileBrowser == null);

                    Precondition.Expect(
                        this.bindingContext != null,
                        "Control must be bound");

                    //
                    // Show in bottom third.
                    //
                    this.container.SplitterDistance = this.Height * 2 / 3;
                    this.container.Panel2Collapsed = false;

                    _ = OpenFileBrowserAsync();
                }
                else
                {
                    //
                    // Hide.
                    //
                    this.container.Panel2Collapsed = true;

                    if (this.fileBrowser != null)
                    {
                        //
                        // Dispose the file browser control and its
                        // underlying channel.
                        //
                        this.Controls.Remove(this.fileBrowser);
                        this.fileBrowser.Dispose();
                        this.fileBrowser = null;
                    }
                }

                async Task OpenFileBrowserAsync()
                {
                    //
                    // Open SFTP channel using the existing connection.
                    //
                    var fsChannel = await this.Connection
                        .OpenFileSystemAsync()
                        .ConfigureAwait(true);

                    //
                    // Bind it to a file browser control.
                    //
                    this.fileBrowser = new FileBrowser()
                    {
                        Dock = DockStyle.Fill,
                    };
                    this.fileBrowserPanel.Controls.Add(this.fileBrowser);

                    var fileSystem = new SftpFileSystem(fsChannel);
                    this.fileBrowser.Disposed += (_, args) => fileSystem.Dispose();
                    this.fileBrowser.Bind(
                        fileSystem, 
                        this.bindingContext!);

                    //
                    // Move focus away from terminal to file browser.
                    // This implicily causes the root directory to
                    // be populated.
                    //

                    this.fileBrowser.Select();
                    this.fileBrowser.Focus();



                    // TODO: Handle NavigationFailed
                }
            }
        }

        public void BrowseFiles()
        {
            if (!this.CanShowFileBrowser) 
            { 
                return; 
            }

            this.IsFileBrowserVisible = true;
        }

        // TODO: paste icon
    }
}
