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
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.ObjectModel
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestCommandContainerForContextMenu : ApplicationFixtureBase
    {
        private Form form;
        private ContextMenuStrip contextMenu;
        private CommandContainer<string> commandContainer;

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

            this.commandContainer = new CommandContainer<string>(
                this.form,
                this.contextMenu.Items,
                ToolStripItemDisplayStyle.ImageAndText,
                new Mock<IServiceProvider>().Object);
        }

        [TearDown]
        public void TearDown()
        {
            this.form.Close();
        }

        //---------------------------------------------------------------------
        // Top-level commands.
        //---------------------------------------------------------------------

        [Test]
        public void WhenQueryStateReturnsUnavailable_ThenMenuItemIsHidden()
        {
            this.commandContainer.AddCommand(
                new Command<string>(
                    "test",
                    ctx => CommandState.Unavailable,
                    ctx => new InvalidOperationException()));

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
                    ctx => new InvalidOperationException()));

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
                    ctx => new InvalidOperationException()));

            var menuItem = this.contextMenu.Items
                .OfType<ToolStripMenuItem>()
                .First(i => i.Text == "test");
            this.commandContainer.Context = "ctx";
            this.contextMenu.Show();

            Assert.IsTrue(menuItem.Visible);
            Assert.IsTrue(menuItem.Enabled);
            Assert.IsNull(menuItem.ToolTipText);
        }

        [Test]
        public void WhenKeyIsUnknown_ThenExecuteCommandByKeyDoesNothing()
        {
            this.commandContainer.ExecuteCommandByKey(Keys.A);
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
        // Second-level commands.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSubMenuAdded_ThenContextIsShared()
        {
            var parentMenu = this.commandContainer.AddCommand(
                new Command<string>(
                    "parent",
                    ctx => CommandState.Enabled,
                    ctx => new InvalidOperationException()));
            var subMenu = parentMenu.AddCommand(
                    new Command<string>(
                        "test",
                        ctx => CommandState.Disabled,
                        ctx => new InvalidOperationException()));

            parentMenu.Context = "ctx";
            Assert.AreEqual("ctx", parentMenu.Context);
            Assert.AreEqual("ctx", subMenu.Context);
        }

        //---------------------------------------------------------------------
        // Default commands.
        //---------------------------------------------------------------------

        [Test]
        public void WhenContainerDoesNotHaveDefaultCommand_ThenExecuteDefaultCommandDoesNothing()
        {
            this.commandContainer.AddCommand(
                new Command<string>(
                    "test",
                    ctx => CommandState.Enabled,
                    ctx =>
                    {
                        Assert.Fail();
                    })
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
                    ctx =>
                    {
                        Assert.Fail();
                    })
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

            this.commandContainer.ExecuteDefaultCommand();
            Assert.IsTrue(commandExecuted);
        }
    }
}
