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

using Google.Solutions.Mvvm.Binding;
using Moq;
using NUnit.Framework;
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
        private class TestForm : Form
        {
            public TestForm()
            {
                this.ContextMenuStrip = new ContextMenuStrip();
            }
        }


        //---------------------------------------------------------------------
        // BindItem.
        //---------------------------------------------------------------------

        [Test]
        public void BindItem_WhenItemBound_ThenMenuPropertiesAreUpdated()
        {
            using (var form = new TestForm())
            {
                form.Show();

                var menuItem = new ToolStripMenuItem();
                form.ContextMenuStrip.Items.Add(menuItem);

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
                    new Mock<IBindingContext>().Object);

                Assert.AreEqual(model.Text, menuItem.Text);
                Assert.AreEqual(model.ToolTip, menuItem.ToolTipText);
                Assert.AreEqual(model.Image, menuItem.Image);
                Assert.AreEqual(model.ShortcutKeys, menuItem.ShortcutKeys);
                Assert.AreEqual(model.IsEnabled, menuItem.Enabled);
                Assert.AreEqual(model.Style, menuItem.DisplayStyle);
            }
        }

        [Test]
        public void BindItem_WhenItemBoundAndPropertiesChange_ThenMenuPropertiesAreUpdated()
        {
            using (var form = new TestForm())
            {
                form.Show();

                var menuItem = new ToolStripMenuItem();
                form.ContextMenuStrip.Items.Add(menuItem);

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
                    new Mock<IBindingContext>().Object);

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
        }

        //---------------------------------------------------------------------
        // BindCollection.
        //---------------------------------------------------------------------

        [Test]
        public void BindCollection_WhenCollectionBound_ThenMenuItemsAreUpdated()
        {
            using (var form = new TestForm())
            {
                form.Show();

                var menuItem = new ToolStripMenuItem("do not touch me");
                form.ContextMenuStrip.Items.Add(menuItem);

                var model = new ObservableCollection<Observable>();
                for (var i = 0; i < 10; i++)
                {
                    model.Add(new Observable()
                    {
                        Text = $"item #{i}",
                    });
                }

                form.ContextMenuStrip.Items.BindCollection(
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
                    new Mock<IBindingContext>().Object);

                Assert.AreEqual(model.Count + 1, form.ContextMenuStrip.Items.Count);
            }
        }

        [Test]
        public void BindCollection_WhenCollectionWithSeparatorBound_ThenMenuItemsAreUpdated()
        {
            var model = new ObservableCollection<Observable>
            {
                new Observable()
                {
                    IsSeparator = true
                }
            };


            using (var form = new TestForm())
            {
                form.Show();
                form.ContextMenuStrip.Items.BindCollection(
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
                    new Mock<IBindingContext>().Object);

                Assert.AreEqual(1, form.ContextMenuStrip.Items.Count);
                Assert.IsInstanceOf<ToolStripSeparator>(form.ContextMenuStrip.Items[0]);
            }
        }

        [Test]
        public void BindCollection_WhenCollectionBoundAndItemInserted_ThenMenuItemsAreUpdated()
        {
            var menuItem = new ToolStripMenuItem("do not touch me");

            var model = new ObservableCollection<Observable>();
            for (var i = 0; i < 3; i++)
            {
                model.Add(new Observable()
                {
                    Text = $"item #{i}",
                });
            }


            using (var form = new TestForm())
            {
                form.Show();
                form.ContextMenuStrip.Items.BindCollection(
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
                    new Mock<IBindingContext>().Object);

                model.Insert(1, new Observable()
                {
                    Text = "new item"
                });

                Assert.AreEqual(4, form.ContextMenuStrip.Items.Count);

                CollectionAssert.AreEquivalent(
                    new[] {
                        "item #0",
                        "new item",
                        "item #1",
                        "item #2"},
                    form.ContextMenuStrip.Items
                        .OfType<ToolStripMenuItem>()
                        .Select(i => i.Text)
                        .ToList());
            }
        }

        [Test]
        public void BindCollection_WhenCollectionBoundAndItemRemoved_ThenMenuItemsAreUpdated()
        {
            var menuItem = new ToolStripMenuItem("do not touch me");

            var model = new ObservableCollection<Observable>();
            for (var i = 0; i < 3; i++)
            {
                model.Add(new Observable()
                {
                    Text = $"item #{i}",
                });
            }


            using (var form = new TestForm())
            {
                form.Show();
                form.ContextMenuStrip.Items.BindCollection(
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
                    new Mock<IBindingContext>().Object);

                model.RemoveAt(1);

                Assert.AreEqual(2, form.ContextMenuStrip.Items.Count);

                CollectionAssert.AreEquivalent(
                    new[] {
                        "item #0",
                        "item #2"},
                    form.ContextMenuStrip.Items
                        .OfType<ToolStripMenuItem>()
                        .Select(i => i.Text)
                        .ToList());
            }
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
