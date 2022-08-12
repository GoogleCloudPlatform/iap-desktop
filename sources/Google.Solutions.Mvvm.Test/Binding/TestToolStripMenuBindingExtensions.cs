﻿//
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

using Google.Solutions.Mvvm.Binding;
using NUnit.Framework;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Binding
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestToolStripMenuBindingExtensions
    {
        private Form form;
        private ContextMenuStrip contextMenu;

        [SetUp]
        public void SetUp()
        {
            this.contextMenu = new ContextMenuStrip();

            this.form = new Form
            {
                ContextMenuStrip = this.contextMenu
            };
            this.form.Show();
        }

        [TearDown]
        public void TearDown()
        {
            this.form.Close();
        }

        //---------------------------------------------------------------------
        // BindItem.
        //---------------------------------------------------------------------

        [Test]
        public void WhenItemBound_ThenMenuPropertiesAreUpdated()
        {
            var menuItem = new ToolStripMenuItem();
            this.contextMenu.Items.Add(menuItem);

            var model = new Observable()
            {
                Text = "text",
                ToolTip = "tooltip",
                ShortcutKeys = Keys.F1,
                IsVisible = true,
                IsEnabled = false
            };

            menuItem.BindItem(
                model,
                m => m.IsSeparator,
                m => m.Text,
                m => m.ToolTip,
                m => m.Image,
                m => m.ShortcutKeys,
                m => m.IsVisible,
                m => m.IsEnabled,
                m => m.Style,
                m => m.Children,
                _ => { },
                null);
            
            Assert.AreEqual(model.Text, menuItem.Text);
            Assert.AreEqual(model.ToolTip, menuItem.ToolTipText);
            Assert.AreEqual(model.Image, menuItem.Image);
            Assert.AreEqual(model.ShortcutKeys, menuItem.ShortcutKeys);
            Assert.AreEqual(model.IsEnabled, menuItem.Enabled);
            Assert.AreEqual(model.Style, menuItem.DisplayStyle);
        }

        [Test]
        public void WhenItemBoundAndPropertiesChange_ThenMenuPropertiesAreUpdated()
        {
            var menuItem = new ToolStripMenuItem();
            this.contextMenu.Items.Add(menuItem);

            var model = new Observable()
            {
                Text = "text",
                ToolTip = "tooltip",
                ShortcutKeys = Keys.F1,
                IsVisible = true,
                IsEnabled = true
            };

            menuItem.BindItem(
                model,
                m => m.IsSeparator,
                m => m.Text,
                m => m.ToolTip,
                m => m.Image,
                m => m.ShortcutKeys,
                m => m.IsVisible,
                m => m.IsEnabled,
                m => m.Style,
                m => m.Children,
                _ => { },
                null);

            model.Text = "new text";
            model.ToolTip = "new tooltip";
            model.ShortcutKeys = Keys.F2;
            model.IsVisible = false;
            model.IsEnabled = false;

            Assert.AreEqual(model.Text, menuItem.Text);
            Assert.AreEqual(model.ToolTip, menuItem.ToolTipText);
            Assert.AreEqual(model.Image, menuItem.Image);
            Assert.AreEqual(model.ShortcutKeys, menuItem.ShortcutKeys);
            Assert.AreEqual(model.IsVisible, menuItem.Visible);
            Assert.AreEqual(model.IsEnabled, menuItem.Enabled);
            Assert.AreEqual(model.Style, menuItem.DisplayStyle);
        }

        //---------------------------------------------------------------------
        // BindCollection.
        //---------------------------------------------------------------------

        [Test]
        public void WhenCollectionBound_ThenMenuItemsAreUpdated()
        {
            var menuItem = new ToolStripMenuItem("do not touch me");
            this.contextMenu.Items.Add(menuItem);

            var model = new ObservableCollection<Observable>();
            for (int i = 0; i < 10; i++)
            {
                model.Add(new Observable()
                {
                    Text = $"item #{i}",
                });
            }

            this.contextMenu.Items.BindCollection(
                model,
                m => m.IsSeparator,
                m => m.Text,
                m => m.ToolTip,
                m => m.Image,
                m => m.ShortcutKeys,
                m => m.IsVisible,
                m => m.IsEnabled,
                m => m.Style,
                m => m.Children,
                _ => { },
                null);

            Assert.AreEqual(model.Count + 1, this.contextMenu.Items.Count);
        }

        [Test]
        public void WhenCollectionWithSeparatorBound_ThenMenuItemsAreUpdated()
        {
            var model = new ObservableCollection<Observable>();
            model.Add(new Observable()
            {
                IsSeparator = true
            });

            this.contextMenu.Items.BindCollection(
                model,
                m => m.IsSeparator,
                m => m.Text,
                m => m.ToolTip,
                m => m.Image,
                m => m.ShortcutKeys,
                m => m.IsVisible,
                m => m.IsEnabled,
                m => m.Style,
                m => m.Children,
                _ => { },
                null);

            Assert.AreEqual(1, this.contextMenu.Items.Count);
            Assert.IsInstanceOf<ToolStripSeparator>(this.contextMenu.Items[0]);
        }

        [Test]
        public void WhenCollectionBoundAndItemInserted_ThenMenuItemsAreUpdated()
        {
            var menuItem = new ToolStripMenuItem("do not touch me");

            var model = new ObservableCollection<Observable>();
            for (int i = 0; i < 3; i++)
            {
                model.Add(new Observable()
                {
                    Text = $"item #{i}",
                });
            }

            this.contextMenu.Items.BindCollection(
                model,
                m => m.IsSeparator,
                m => m.Text,
                m => m.ToolTip,
                m => m.Image,
                m => m.ShortcutKeys,
                m => m.IsVisible,
                m => m.IsEnabled,
                m => m.Style,
                m => m.Children,
                _ => { },
                null);

            model.Insert(1, new Observable()
            {
                Text = "new item"
            });

            Assert.AreEqual(4, this.contextMenu.Items.Count);

            CollectionAssert.AreEquivalent(
                new[] {
                    "item #0",
                    "new item",
                    "item #1",
                    "item #2"},
                this.contextMenu.Items
                    .OfType<ToolStripMenuItem>()
                    .Select(i => i.Text)
                    .ToList());
        }

        [Test]
        public void WhenCollectionBoundAndItemRemoved_ThenMenuItemsAreUpdated()
        {
            var menuItem = new ToolStripMenuItem("do not touch me");

            var model = new ObservableCollection<Observable>();
            for (int i = 0; i < 3; i++)
            {
                model.Add(new Observable()
                {
                    Text = $"item #{i}",
                });
            }

            this.contextMenu.Items.BindCollection(
                model,
                m => m.IsSeparator,
                m => m.Text,
                m => m.ToolTip,
                m => m.Image,
                m => m.ShortcutKeys,
                m => m.IsVisible,
                m => m.IsEnabled,
                m => m.Style,
                m => m.Children,
                _ => { },
                null);

            model.RemoveAt(1);

            Assert.AreEqual(2, this.contextMenu.Items.Count);

            CollectionAssert.AreEquivalent(
                new[] {
                    "item #0",
                    "item #2"},
                this.contextMenu.Items
                    .OfType<ToolStripMenuItem>()
                    .Select(i => i.Text)
                    .ToList());
        }

        private class Observable : ViewModelBase
        {
            private string text;
            private string toolTip;
            private Image image;
            private Keys shortcutKeys;
            private bool isVisible;
            private bool isEnabled;
            private bool isSeparator;

            public ToolStripItemDisplayStyle Style => ToolStripItemDisplayStyle.Text;

            public ObservableCollection<Observable> Children { get; set; }

            public string Text
            {
                get => this.text;
                set
                {
                    this.text = value;
                    RaisePropertyChange();
                }
            }

            public string ToolTip
            {
                get => this.toolTip;
                set
                {
                    this.toolTip = value;
                    RaisePropertyChange();
                }
            }

            public Image Image
            {
                get => this.image;
                set
                {
                    this.image = value;
                    RaisePropertyChange();
                }
            }

            public Keys ShortcutKeys
            {
                get => this.shortcutKeys;
                set
                {
                    this.shortcutKeys = value;
                    RaisePropertyChange();
                }
            }

            public bool IsVisible
            {
                get => this.isVisible;
                set
                {
                    this.isVisible = value;
                    RaisePropertyChange();
                }
            }

            public bool IsEnabled
            {
                get => this.isEnabled;
                set
                {
                    this.isEnabled = value;
                    RaisePropertyChange();
                }
            }

            public bool IsSeparator
            {
                get => this.isSeparator;
                set
                {
                    this.isSeparator = value;
                    RaisePropertyChange();
                }
            }
        }
    }
}
