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

using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Properties;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Rdp;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;
using Google.Solutions.Mvvm.Binding.Commands;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views
{
    [Service]
    public class SessionCommands
    {
        public SessionCommands(
            UrlCommands urlCommands,
            Service<IRdpConnectionService> connectionService)
        {
            //
            // Install command for launching URLs.
            //
            urlCommands.LaunchRdpUrl = new LaunchRdpUrlCommand(connectionService);

            this.EnterFullScreenOnSingleScreen = new FullScreenCommand(
                "&Full screen",
                FullScreenMode.SingleScreen)
            {
                Image = Resources.Fullscreen_16,
                ShortcutKeys = DocumentWindow.EnterFullScreenHotKey,
                ActivityText = "Activating full screen"
            };
            this.EnterFullScreenOnAllScreens = new FullScreenCommand(
                "&Full screen (multiple displays)",
                FullScreenMode.AllScreens)
            {
                Image = Resources.Fullscreen_16,
                ShortcutKeys = Keys.F11 | Keys.Shift,
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

        //---------------------------------------------------------------------
        // RDP URL commands.
        //---------------------------------------------------------------------

        private class LaunchRdpUrlCommand : ToolContextCommand<IapRdpUrl>
        {
            private readonly Service<IRdpConnectionService> connectionService;

            public LaunchRdpUrlCommand(
                Service<IRdpConnectionService> connectionService)
                : base("Launch &RDP URL")
            {
                this.connectionService = connectionService;
            }

            protected override bool IsAvailable(IapRdpUrl url)
            {
                return url != null;
            }

            protected override bool IsEnabled(IapRdpUrl url)
            {
                return url != null;
            }

            public override Task ExecuteAsync(IapRdpUrl url)
            {
                return this.connectionService
                    .GetInstance()
                    .ActivateOrConnectInstanceAsync(url);
            }
        }

        //---------------------------------------------------------------------
        // Generic session commands.
        //---------------------------------------------------------------------

        private abstract class SessionCommandBase : ToolContextCommand<ISession>
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
                    session is IRemoteDesktopSession rdpSession &&
                    rdpSession.IsConnected && 
                    rdpSession.CanEnterFullScreen;
            }

            public override void Execute(ISession session)
            {
                var rdpSession = (IRemoteDesktopSession)session;
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
                    session is IRemoteDesktopSession rdpSession &&
                    rdpSession.IsConnected;
            }

            public override void Execute(ISession session)
            {
                var rdpSession = (IRemoteDesktopSession)session;
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
                    session is IRemoteDesktopSession rdpSession &&
                    rdpSession.IsConnected;
            }

            public override void Execute(ISession session)
            {
                var rdpSession = (IRemoteDesktopSession)session;
                rdpSession.ShowTaskManager();
            }
        }

        //---------------------------------------------------------------------
        // SSH session commands.
        //---------------------------------------------------------------------

        private class DownloadFilesCommand : SessionCommandBase
        {
            public DownloadFilesCommand(string text) : base(text)
            {
            }

            protected override bool IsEnabled(ISession session)
            {
                return session != null &&
                    session is ISshTerminalSession sshSession &&
                    sshSession.IsConnected;
            }

            public override Task ExecuteAsync(ISession session)
            {
                var sshSession = (ISshTerminalSession)session;
                return sshSession.DownloadFilesAsync();
            }
        }
    }
}
