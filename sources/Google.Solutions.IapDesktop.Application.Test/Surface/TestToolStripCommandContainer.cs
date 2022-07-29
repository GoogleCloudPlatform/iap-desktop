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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Surface;
using Google.Solutions.Testing.Application.Test;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.Surface
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestToolStripCommandContainer : ApplicationFixtureBase
    {
        private Form form;
        private ContextMenuStrip contextMenu;
        private ToolStripCommandContainer<string> commandContainer;

        [SetUp]
        public void SetUp()
        {
            this.contextMenu = new ContextMenuStrip();

            this.form = new Form
            {
                ContextMenuStrip = this.contextMenu
            };
            this.contextMenu.Items.Add(new ToolStripSeparator());
            this.form.Show();

            this.commandContainer = new ToolStripCommandContainer<string>(
                ToolStripItemDisplayStyle.ImageAndText);
            this.commandContainer.ApplyTo(this.contextMenu);
        }

        [TearDown]
        public void TearDown()
        {
            this.form.Close();
        }

        //---------------------------------------------------------------------
        // Menu enabling/disabling.
        //---------------------------------------------------------------------

        [Test]
        public void WhenQueryStateReturnsUnavailable_ThenMenuItemIsHidden()
        {
            this.commandContainer.AddCommand(
                new Command<string>(
                    "test",
                    ctx => CommandState.Unavailable,
                    ctx => throw new InvalidOperationException()));

            var menuItem = this.contextMenu.Items
                .OfType<ToolStripMenuItem>()
                .First(i => i.Text == "test");
            this.commandContainer.Context = "ctx";
            this.contextMenu.Show();

            Assert.IsFalse(menuItem.Visible);
        }

        [Test]
        public void WhenQueryStateReturnsDisabled_ThenMenuItemIsDisabled()
        {
            this.commandContainer.AddCommand(
                new Command<string>(
                    "test",
                    ctx => CommandState.Disabled,
                    ctx => throw new InvalidOperationException()));

            var menuItem = this.contextMenu.Items
                .OfType<ToolStripMenuItem>()
                .First(i => i.Text == "test");
            this.commandContainer.Context = "ctx";
            this.contextMenu.Show();

            Assert.IsTrue(menuItem.Visible);
            Assert.IsFalse(menuItem.Enabled);
        }

        [Test]
        public void WhenQueryStateReturnsEnabled_ThenMenuItemIsEnabled()
        {
            this.commandContainer.AddCommand(
                new Command<string>(
                    "test",
                    ctx => CommandState.Enabled,
                    ctx => throw new InvalidOperationException()));

            var menuItem = this.contextMenu.Items
                .OfType<ToolStripMenuItem>()
                .First(i => i.Text == "test");
            this.commandContainer.Context = "ctx";
            this.contextMenu.Show();

            Assert.IsTrue(menuItem.Visible);
            Assert.IsTrue(menuItem.Enabled);
            Assert.IsNull(menuItem.ToolTipText);
        }

        //---------------------------------------------------------------------
        // ExecuteCommandByKey.
        //---------------------------------------------------------------------

        [Test]
        public void WhenKeyIsUnknown_ThenExecuteCommandByKeyDoesNothing()
        {
            this.commandContainer.ExecuteCommandByKey(Keys.A);
        }

        [Test]
        public void WhenContextIsNull_ThenExecuteCommandByKeyDoesNothing()
        {
            this.commandContainer.AddCommand(
                new Command<string>(
                    "test",
                    ctx => CommandState.Enabled,
                    ctx => Assert.Fail("Unexpected callback")));

            this.commandContainer.Context = null;
            this.commandContainer.ExecuteCommandByKey(Keys.F4);
        }

        [Test]
        public void WhenKeyIsMappedAndCommandIsEnabled_ThenExecuteCommandInvokesHandler()
        {
            string contextOfCallback = null;
            this.commandContainer.AddCommand(
                new Command<string>(
                    "test",
                    ctx => CommandState.Enabled,
                    ctx =>
                    {
                        contextOfCallback = ctx;
                    })
                {
                    ShortcutKeys = Keys.F4
                });

            this.commandContainer.Context = "foo";

            this.commandContainer.ExecuteCommandByKey(Keys.F4);

            Assert.AreEqual("foo", contextOfCallback);
        }

        [Test]
        public void WhenKeyIsMappedAndCommandIsDisabled_ThenExecuteCommandByKeyDoesNothing()
        {
            this.commandContainer.AddCommand(
                new Command<string>(
                    "test",
                    ctx => CommandState.Disabled,
                    ctx =>
                    {
                        Assert.Fail();
                    })
                {
                    ShortcutKeys = Keys.F4
                });

            this.commandContainer.Context = "foo";

            this.commandContainer.ExecuteCommandByKey(Keys.F4);
        }

        //---------------------------------------------------------------------
        // ForceRefresh.
        //---------------------------------------------------------------------

        [Test]
        public void WhenContainerHasCommands_ThenForceRefreshCallsQueryState()
        {
            int queryCalls = 0;
            this.commandContainer.AddCommand(
                new Command<string>(
                    "test",
                    ctx =>
                    {
                        Assert.AreEqual("ctx", ctx);
                        queryCalls++;
                        return CommandState.Disabled;
                    },
                    ctx => throw new InvalidOperationException()));

            this.commandContainer.Context = "ctx";
            this.contextMenu.Show();

            Assert.AreEqual(1, queryCalls);

            ((CommandContainer<string>)this.commandContainer).ForceRefresh();

            Assert.AreEqual(2, queryCalls);
        }

        //---------------------------------------------------------------------
        // Second-level commands.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSubMenuAdded_ThenContextIsShared()
        {
            var parentMenu = (CommandContainer<string>)this.commandContainer.AddCommand(
                new Command<string>(
                    "parent",
                    ctx => CommandState.Enabled,
                    ctx => throw new InvalidOperationException()));
            var subMenu = (CommandContainer<string>)parentMenu.AddCommand(
                    new Command<string>(
                        "test",
                        ctx => CommandState.Disabled,
                        ctx => throw new InvalidOperationException()));

            parentMenu.Context = "ctx";
            Assert.AreEqual("ctx", parentMenu.Context);
            Assert.AreEqual("ctx", subMenu.Context);
        }

        [Test]
        public void WhenSubCommandsAdded_ThenOnMenuItemsChangedEventIsRaised()
        {
            var root = this.commandContainer;

            int eventsReceived = 0;
            root.MenuItemsChanged += (s, a) =>
            {
                eventsReceived++;
            };

            var parentMenu = root.AddCommand(
                new Command<string>(
                    "parent",
                    ctx => CommandState.Enabled,
                    ctx => throw new InvalidOperationException()));
            var subMenu = (CommandContainer<string>)parentMenu.AddCommand(
                new Command<string>(
                    "test",
                    ctx => CommandState.Disabled,
                    ctx => throw new InvalidOperationException()));
            subMenu.AddCommand(
                new Command<string>(
                    "test",
                    ctx => CommandState.Disabled,
                    ctx => throw new InvalidOperationException()));
            subMenu.AddSeparator();

            Assert.AreEqual(4, eventsReceived);
        }

        //---------------------------------------------------------------------
        // ExecuteDefaultCommand.
        //---------------------------------------------------------------------

        [Test]
        public void WhenContainerDoesNotHaveDefaultCommand_ThenExecuteDefaultCommandDoesNothing()
        {
            this.commandContainer.AddCommand(
                new Command<string>(
                    "test",
                    ctx => CommandState.Enabled,
                    ctx => Assert.Fail("Unexpected callback"))
                {
                });

            this.commandContainer.ExecuteDefaultCommand();
        }

        [Test]
        public void WhenDefaultCommandIsDisabled_ThenExecuteDefaultCommandDoesNothing()
        {

            this.commandContainer.AddCommand(
                new Command<string>(
                    "test",
                    ctx => CommandState.Disabled,
                    ctx => Assert.Fail("Unexpected callback"))
                {
                    IsDefault = true
                });

            this.commandContainer.ExecuteDefaultCommand();
        }

        [Test]
        public void WhenContextIsNull_ThenExecuteDefaultCommandDoesNothing()
        {

            this.commandContainer.AddCommand(
                new Command<string>(
                    "test",
                    ctx => CommandState.Enabled,
                    ctx => Assert.Fail("Unexpected callback"))
                {
                    IsDefault = true
                });

            this.commandContainer.ExecuteDefaultCommand();
        }

        [Test]
        public void WhenDefaultCommandIsEnabled_ThenExecuteDefaultExecutesCommand()
        {
            bool commandExecuted = false;
            this.commandContainer.AddCommand(
                new Command<string>(
                    "test",
                    ctx => CommandState.Enabled,
                    ctx =>
                    {
                        commandExecuted = true;
                    })
                {
                    IsDefault = true
                });

            this.commandContainer.Context = "test";
            this.commandContainer.ExecuteDefaultCommand();
            Assert.IsTrue(commandExecuted);
        }
    }
}
