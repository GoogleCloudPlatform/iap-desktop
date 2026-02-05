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

using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestSessionPaneBase
    {
        //---------------------------------------------------------------------
        // Context menu.
        //---------------------------------------------------------------------

        private class SessionPane : SessionViewBase
        {
            public SessionPane() : base(new Mock<IBindingContext>().Object)
            {
            }
        }

        [Test]
        public void ContextMenu_WhenContextCommandsSet_ThenCommandsAreBoundToContextMenu()
        {
            var commands = new CommandContainer<ISession>(
                ToolStripItemDisplayStyle.Text,
                new Mock<IContextSource<ISession>>().Object,
                new Mock<IBindingContext>().Object);
            commands.AddCommand(
                new ContextCommand<ISession>(
                    "test-command",
                    s => CommandState.Enabled,
                    s => { }));


            using (var window = new SessionPane()
            {
                ContextCommands = commands
            })
            {

                Assert.IsNotNull(window.ContextCommands);
                Assert.IsNotNull(window.TabPageContextMenuStrip);

                Assert.That(
                    window.TabPageContextMenuStrip.Items
                        .Cast<ToolStripMenuItem>()
                        .Select(i => i.Text)
                        .ToList(), Has.Member("test-command"));
            }
        }

        [Test]
        public void ContextMenu_WhenContextCommandsSet_ThenSettingCommandsAgainCausesException()
        {
            var commands = new CommandContainer<ISession>(
                ToolStripItemDisplayStyle.Text,
                new Mock<IContextSource<ISession>>().Object,
                new Mock<IBindingContext>().Object);
            commands.AddCommand(
                new ContextCommand<ISession>(
                    "test-command",
                    s => CommandState.Enabled,
                    s => { }));

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
        public void ContextMenu_WhenContextCommandsSet_ThenDefaultCloseMenuIsReplaced()
        {
            using (var window = new SessionPane())
            {
                window.Show();

                var commands = new CommandContainer<ISession>(
                    ToolStripItemDisplayStyle.Text,
                    new Mock<IContextSource<ISession>>().Object,
                    new Mock<IBindingContext>().Object);
                commands.AddCommand(
                    new ContextCommand<ISession>(
                        "test-command",
                        s => CommandState.Enabled,
                        s => { }));
                window.ContextCommands = commands;
                Assert.That(window.ShowCloseMenuItemInContextMenu, Is.False);

                window.Close();
            }
        }
    }
}
