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

using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Properties;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Rdp;
using Google.Solutions.Mvvm.Binding.Commands;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session
{
    [Service]
    public class SessionCommands
    {
        public SessionCommands()
        {
            this.EnterFullScreenOnSingleScreen = new FullScreenCommand(
                "&Full screen",
                FullScreenMode.SingleScreen)
            {
                Image = Resources.Fullscreen_16,
                ActivityText = "Activating full screen"

                //
                // NB. Don't set shortcut key here as the RDP control
                // traps the key already.
                //
            };
            this.EnterFullScreenOnAllScreens = new FullScreenCommand(
                "&Full screen (multiple displays)",
                FullScreenMode.AllScreens)
            {
                Image = Resources.Fullscreen_16,
                ActivityText = "Activating full screen"
            };
            this.Disconnect = new DisconnectCommand("&Disconnect")
            {
                Image = Resources.Disconnect_16,
                ShortcutKeys = Keys.Control | Keys.F4,
                ActivityText = "Disconnecting"
            };
            this.ShowSecurityScreen = new ShowSecurityScreenCommand(
                "Show &security screen (send Ctrl+Alt+Esc)");
            this.ShowTaskManager = new ShowTaskManagerCommand(
                "Open &task manager (send Ctrl+Shift+Esc)");
            this.DownloadFiles = new DownloadFilesCommand("Do&wnload files...")
            {
                Image = Resources.DownloadFile_16,
                ActivityText = "Downloading files"
            };
            this.UploadFiles = new UploadFilesCommand("U&pload files...")
            {
                Image = Resources.UploadFile_16,
                ActivityText = "Uploading files"
            };
        }

        //---------------------------------------------------------------------
        // Context commands.
        //---------------------------------------------------------------------

        public IContextCommand<ISession> EnterFullScreenOnSingleScreen { get; }
        public IContextCommand<ISession> EnterFullScreenOnAllScreens { get; }
        public IContextCommand<ISession> Disconnect { get; }
        public IContextCommand<ISession> ShowSecurityScreen { get; }
        public IContextCommand<ISession> ShowTaskManager { get; }
        public IContextCommand<ISession> DownloadFiles { get; }
        public IContextCommand<ISession> UploadFiles { get; }

        //---------------------------------------------------------------------
        // Generic session commands.
        //---------------------------------------------------------------------

        private abstract class SessionCommandBase : MenuCommandBase<ISession>
        {
            protected SessionCommandBase(string text) : base(text)
            {
            }

            protected override bool IsAvailable(ISession session)
            {
                return true; // Always available, but possibly disabled.
            }
        }

        private class DisconnectCommand : SessionCommandBase
        {
            public DisconnectCommand(string text) : base(text)
            {
            }

            protected override bool IsEnabled(ISession session)
            {
                return session != null && session.IsConnected;
            }

            public override void Execute(ISession session)
            {
                session.Close();
            }
        }

        private class DownloadFilesCommand : SessionCommandBase
        {
            public DownloadFilesCommand(string text) : base(text)
            {
            }

            protected override bool IsEnabled(ISession session)
            {
                return session != null &&
                    session.IsConnected &&
                    session.CanTransferFiles;
            }

            public override Task ExecuteAsync(ISession session)
            {
                return session.DownloadFilesAsync();
            }
        }

        private class UploadFilesCommand : SessionCommandBase
        {
            public UploadFilesCommand(string text) : base(text)
            {
            }

            protected override bool IsEnabled(ISession session)
            {
                return session != null &&
                    session.IsConnected &&
                    session.CanTransferFiles;
            }

            public override Task ExecuteAsync(ISession session)
            {
                return session.UploadFilesAsync();
            }
        }

        //---------------------------------------------------------------------
        // RDP session commands.
        //---------------------------------------------------------------------

        private class FullScreenCommand : SessionCommandBase
        {
            private readonly FullScreenMode mode;

            public FullScreenCommand(
                string text,
                FullScreenMode mode) : base(text)
            {
                this.mode = mode;
            }

            protected override bool IsEnabled(ISession session)
            {
                return session != null &&
                    session is IRdpSession rdpSession &&
                    rdpSession.IsConnected &&
                    rdpSession.CanEnterFullScreen;
            }

            public override void Execute(ISession session)
            {
                var rdpSession = (IRdpSession)session;
                rdpSession.TrySetFullscreen(this.mode);
            }
        }

        private class ShowSecurityScreenCommand : SessionCommandBase
        {
            public ShowSecurityScreenCommand(string text) : base(text)
            {
            }

            protected override bool IsEnabled(ISession session)
            {
                return session != null &&
                    session is IRdpSession rdpSession &&
                    rdpSession.IsConnected;
            }

            public override void Execute(ISession session)
            {
                var rdpSession = (IRdpSession)session;
                rdpSession.ShowSecurityScreen();
            }
        }

        private class ShowTaskManagerCommand : SessionCommandBase
        {
            public ShowTaskManagerCommand(string text) : base(text)
            {
            }

            protected override bool IsEnabled(ISession session)
            {
                return session != null &&
                    session is IRdpSession rdpSession &&
                    rdpSession.IsConnected;
            }

            public override void Execute(ISession session)
            {
                var rdpSession = (IRdpSession)session;
                rdpSession.ShowTaskManager();
            }
        }
    }
}
