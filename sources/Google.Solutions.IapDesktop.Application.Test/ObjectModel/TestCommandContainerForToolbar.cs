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

#pragma warning disable CA1806 // Do not ignore method results

namespace Google.Solutions.IapDesktop.Application.Test.ObjectModel
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestCommandContainerForToolbar : ApplicationFixtureBase
    {
        private Form form;
        private ToolStrip toolStrip;
        private CommandContainer<string> commandContainer;

        [SetUp]
        public void SetUp()
        {
            this.toolStrip = new ToolStrip();

            this.form = new Form();
            this.form.Show();

            this.commandContainer = new CommandContainer<string>(
                this.form,
                this.toolStrip.Items,
                ToolStripItemDisplayStyle.Image,
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
        public void WhenQueryStateReturnsDisabled_ThenMenuItemIsDisabled()
        {
            this.commandContainer.AddCommand(
                new Command<string>(
                    "test",
                    ctx => CommandState.Disabled,
                    ctx => new InvalidOperationException()));

            var menuItem = this.toolStrip.Items
                .OfType<ToolStripMenuItem>()
                .First(i => i.Text == "test");
            this.commandContainer.Context = "ctx";
            this.toolStrip.Show();

            Assert.IsFalse(menuItem.Enabled);
        }

        [Test]
        public void WhenQueryStateReturnsEnabled_ThenMenuItemIsEnabledAndHasToolTip()
        {
            this.commandContainer.AddCommand(
                new Command<string>(
                    "&test",
                    ctx => CommandState.Enabled,
                    ctx => new InvalidOperationException()));

            var menuItem = this.toolStrip.Items
                .OfType<ToolStripMenuItem>()
                .First(i => i.Text == "&test");
            this.commandContainer.Context = "ctx";
            this.toolStrip.Show();

            Assert.IsTrue(menuItem.Enabled);
            Assert.AreEqual(ToolStripItemDisplayStyle.Image, menuItem.DisplayStyle);
            Assert.AreEqual("test", menuItem.ToolTipText);
        }
    }
}
