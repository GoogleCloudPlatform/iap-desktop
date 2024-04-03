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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Properties;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Rdp;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Mvvm.Controls;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session
{
    [Service]
    public class SessionCommands
    {
        public SessionCommands(ISessionBroker sessionBroker)
        {
            this.EnterFullScreenOnSingleScreen = new FullScreenCommand(
                "&Full screen",
                FullScreenMode.SingleScreen);
            this.EnterFullScreenOnAllScreens = new FullScreenCommand(
                "&Full screen (multiple displays)",
                FullScreenMode.AllScreens);
            this.Close = new CloseCommand();
            this.ShowSecurityScreen = new ShowSecurityScreenCommand();
            this.ShowTaskManager = new ShowTaskManagerCommand();
            this.TypeClipboardText = new TypeClipboardTextCommand();
            this.DownloadFiles = new DownloadFilesCommand();
            this.UploadFiles = new UploadFilesCommand();
            this.CloseAll = new CloseAllCommand(sessionBroker);
            this.CloseAllButThis = new CloseAllButThisCommand(sessionBroker);
        }

        //---------------------------------------------------------------------
        // Context commands.
        //---------------------------------------------------------------------

        public IContextCommand<ISession> EnterFullScreenOnSingleScreen { get; }
        public IContextCommand<ISession> EnterFullScreenOnAllScreens { get; }
        public IContextCommand<ISession> Close { get; }
        public IContextCommand<ISession> ShowSecurityScreen { get; }
        public IContextCommand<ISession> ShowTaskManager { get; }
        public IContextCommand<ISession> TypeClipboardText { get; }
        public IContextCommand<ISession> DownloadFiles { get; }
        public IContextCommand<ISession> UploadFiles { get; }
        public IContextCommand<ISession> CloseAll { get; }
        public IContextCommand<ISession> CloseAllButThis { get; }

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

        private class DownloadFilesCommand : SessionCommandBase
        {
            public DownloadFilesCommand()
                : base("Do&wnload files...")
            {
                this.Image = Resources.DownloadFile_16;
                this.ActivityText = "Downloading files";
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
            public UploadFilesCommand()
                : base("U&pload files...")
            {
                this.Image = Resources.UploadFile_16;
                this.ActivityText = "Uploading files";
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

        private class CloseCommand : SessionCommandBase
        {
            public CloseCommand()
                : base("&Close")
            {
                this.Image = Resources.Disconnect_16;
                this.ShortcutKeys = Keys.Control | Keys.F4;
                this.ActivityText = "Disconnecting";
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

        private class CloseAllCommand : SessionCommandBase
        {
            private readonly ISessionBroker broker;

            public CloseAllCommand(ISessionBroker broker)
                : base("Close &all")
            {
                this.broker = broker.ExpectNotNull(nameof(broker));
            }

            protected override bool IsEnabled(ISession _)
            {
                return true;
            }

            public override void Execute(ISession _)
            {
                foreach (var session in this.broker.Sessions)
                {
                    session.Close();
                }
            }
        }

        private class CloseAllButThisCommand : SessionCommandBase
        {
            private readonly ISessionBroker broker;

            public CloseAllButThisCommand(ISessionBroker broker)
                : base("Close &others")
            {
                this.broker = broker.ExpectNotNull(nameof(broker));
            }

            protected override bool IsEnabled(ISession _)
            {
                return true;
            }

            public override void Execute(ISession current)
            {
                foreach (var session in this.broker.Sessions)
                {
                    if (session != current)
                    {
                        session.Close();
                    }
                }
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
                this.Image = Resources.Fullscreen_16;
                this.ActivityText = "Activating full screen";

                //
                // NB. Don't set shortcut key here as the RDP control
                // traps the key already.
                //
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
            public ShowSecurityScreenCommand()
                : base("Show &security screen (send Ctrl+Alt+Esc)")
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
            public ShowTaskManagerCommand()
                : base("Open &task manager (send Ctrl+Shift+Esc)")
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

        private class TypeClipboardTextCommand : SessionCommandBase
        {
            public TypeClipboardTextCommand()
                : base("&Type clipboard text")
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
                rdpSession.SendText(ClipboardUtil.GetText());
            }
        }
    }
}
