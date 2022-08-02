//
// Copyright 2022 Google LLC
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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.ObjectModel.Commands;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Shell.Views;
using Google.Solutions.Testing.Application.ObjectModel;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views
{
    [TestFixture]

    [Apartment(ApartmentState.STA)]
    public class TestSessionPaneBase : ShellFixtureBase
    {
        //---------------------------------------------------------------------
        // Context menu.
        //---------------------------------------------------------------------

        private class SessionPane : SessionPaneBase
        {
            public SessionPane()
            {
            }
        }

        [Test]
        public void WhenContextCommandsSet_ThenCommandsAreBoundToContextMenu()
        {
            var commands = new CommandContainer<ISession>(
                ToolStripItemDisplayStyle.Text,
                new Mock<ICommandContextSource<ISession>>().Object);
            commands.AddCommand(
                "test-command",
                s => CommandState.Enabled,
                s => { });


            using (var window = new SessionPane()
            {
                ContextCommands = commands
            })
            {

                Assert.IsNotNull(window.ContextCommands);
                Assert.IsNotNull(window.TabPageContextMenuStrip);

                CollectionAssert.Contains(
                    window.TabPageContextMenuStrip.Items
                        .Cast<ToolStripMenuItem>()
                        .Select(i => i.Text)
                        .ToList(),
                    "test-command");
            }
        }

        [Test]
        public void WhenContextCommandsSet_ThenSettingCommandsAgainCausesException()
        {
            var commands = new CommandContainer<ISession>(
                ToolStripItemDisplayStyle.Text,
                new Mock<ICommandContextSource<ISession>>().Object);
            commands.AddCommand(
                "test-command",
                s => CommandState.Enabled,
                s => { });

            using (var window = new SessionPane()
            {
                ContextCommands = commands
            })
            {
                Assert.Throws<InvalidOperationException>(
                    () => window.ContextCommands = commands);
            }
        }

        [Test]
        public void WhenContextCommandsSet_ThenDefaultCloseMenuIsReplaced()
        {
            using (var window = new SessionPane())
            {
                window.Show();

                var commands = new CommandContainer<ISession>(
                    ToolStripItemDisplayStyle.Text,
                    new Mock<ICommandContextSource<ISession>>().Object);
                commands.AddCommand(
                    "test-command",
                    s => CommandState.Enabled,
                    s => { });
                window.ContextCommands = commands;
                Assert.IsFalse(window.ShowCloseMenuItemInContextMenu);

                window.Close();
            }
        }
    }
}
