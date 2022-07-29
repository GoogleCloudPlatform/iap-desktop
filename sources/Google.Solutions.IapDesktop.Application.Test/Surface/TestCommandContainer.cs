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
    public class TestCommandContainer : ApplicationFixtureBase
    {

        //---------------------------------------------------------------------
        // Menu enabling/disabling.
        //---------------------------------------------------------------------

        [Test]
        public void WhenQueryStateReturnsDisabled_ThenMenuItemIsDisabled()
        {
            var container = new CommandContainer<string>(ToolStripItemDisplayStyle.Text);
            container.AddCommand(
                new Command<string>(
                    "test",
                    ctx => CommandState.Disabled,
                    ctx => throw new InvalidOperationException()));
            container.ForceRefresh();

            var menuItem = container.MenuItems.First();

            Assert.IsTrue(menuItem.Visible);
            Assert.IsFalse(menuItem.Enabled);
        }

        [Test]
        public void WhenQueryStateReturnsEnabled_ThenMenuItemIsEnabled()
        {
            var container = new CommandContainer<string>(ToolStripItemDisplayStyle.Text);
            container.AddCommand(
                new Command<string>(
                    "test",
                    ctx => CommandState.Enabled,
                    ctx => throw new InvalidOperationException()));

            var menuItem = container.MenuItems.First();

            Assert.IsTrue(menuItem.Visible);
            Assert.IsTrue(menuItem.Enabled);
        }

        [Test]
        public void WhenQueryStateReturnsUnavailable_ThenMenuItemIsHidden()
        {
            var container = new CommandContainer<string>(ToolStripItemDisplayStyle.Text);
            container.AddCommand(
                new Command<string>(
                    "test",
                    ctx => CommandState.Unavailable,
                    ctx => throw new InvalidOperationException()));

            var menuItem = container.MenuItems.First();

            Assert.IsFalse(menuItem.Visible);
        }

        // TODO: Add tests
    }
}
