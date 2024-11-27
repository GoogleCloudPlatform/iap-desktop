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

using Google.Solutions.Mvvm.Theme;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Theme
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestToolStripItemTheme
    {
        [Test]
        public void WhenToolStripIsNull_ThenApplyToReturns()
        {
            var theme = new ToolStripItemTheme(true);
            theme.ApplyTo(null!);
        }

        [Test]
        public void WhenToolStripEmpty_ThenApplyToReturns()
        {
            var theme = new ToolStripItemTheme(true);
            theme.ApplyTo(new MenuStrip());
        }

        //---------------------------------------------------------------------
        // Top-level items.
        //---------------------------------------------------------------------

        [Test]
        public void ApplyTo_ConsidersCurrentTopLevelItems()
        {
            var menu = new MenuStrip();

            var item1 = new ToolStripSeparator();
            var item2 = new ToolStripSeparator();
            menu.Items.Add(item1);
            menu.Items.Add(item2);

            var theme = new ToolStripItemTheme(true);

            var appliedItems = new List<ToolStripItem>();
            theme.AddRule(i => appliedItems.Add(i));
            theme.ApplyTo(menu);

            CollectionAssert.AreEquivalent(
                new[] { item1, item2 },
                appliedItems);
        }

        [Test]
        public void ApplyTo_ConsidersTopLevelItemsAddedLater()
        {
            var menu = new MenuStrip();

            var theme = new ToolStripItemTheme(true);

            var appliedItems = new List<ToolStripItem>();
            theme.AddRule(i => appliedItems.Add(i));
            theme.ApplyTo(menu);

            var item1 = new ToolStripSeparator();
            var item2 = new ToolStripSeparator();
            menu.Items.Add(item1);
            menu.Items.Add(item2);

            CollectionAssert.AreEquivalent(
                new[] { item1, item2 },
                appliedItems);
        }

        //---------------------------------------------------------------------
        // Lower-level items.
        //---------------------------------------------------------------------

        [Test]
        public void ApplyTo_ConsidersCurrentLowerLevelItems()
        {
            var menu = new MenuStrip();

            var level2 = new ToolStripMenuItem();
            var level3 = new ToolStripMenuItem();

            menu.Items.Add(level2);
            level2.DropDownItems.Add(level3);

            var theme = new ToolStripItemTheme(true);

            var appliedItems = new List<ToolStripItem>();
            theme.AddRule(i => appliedItems.Add(i));
            theme.ApplyTo(menu);

            CollectionAssert.AreEquivalent(
                new[] { level2, level3 },
                appliedItems);
        }

        [Test]
        public void ApplyTo_ConsidersSecondLevelItemsAddedLater()
        {
            var menu = new MenuStrip();

            var theme = new ToolStripItemTheme(true);

            var appliedItems = new List<ToolStripItem>();
            theme.AddRule(i => appliedItems.Add(i));
            theme.ApplyTo(menu);

            var level2 = new ToolStripMenuItem();
            menu.Items.Add(level2);

            CollectionAssert.AreEquivalent(
                new[] { level2 },
                appliedItems);
        }

        [Test]
        public void ApplyTo_ConsidersThirdLevelItemsAddedLater()
        {
            var menu = new MenuStrip();

            var level2 = new ToolStripMenuItem();
            menu.Items.Add(level2);

            var theme = new ToolStripItemTheme(true);

            var appliedItems = new List<ToolStripItem>();
            theme.AddRule(i => appliedItems.Add(i));
            theme.ApplyTo(menu);

            var level3 = new ToolStripMenuItem();
            level2.DropDownItems.Add(level3);

            level2.ShowDropDown();

            // Show again
            level2.ShowDropDown();

            CollectionAssert.AreEquivalent(
                new[] { level2, level3 },
                appliedItems);
        }
    }
}
