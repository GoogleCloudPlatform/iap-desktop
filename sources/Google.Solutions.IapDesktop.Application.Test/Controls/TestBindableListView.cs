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

using Google.Solutions.IapDesktop.Application.Controls;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using NUnit.Framework;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestBindableListView : FixtureBase
    {
        private class ModelItem : ViewModelBase
        {
            private string name;
            private int imageIndex;

            public string Name
            {
                get => this.name;
                set
                {
                    this.name = value;
                    RaisePropertyChange();
                }
            }

            public int ImageIndex
            {
                get => this.imageIndex;
                set
                {
                    this.imageIndex = value;
                    RaisePropertyChange();
                }
            }
        }

        private class ModelListView : BindableListView<ModelItem>
        {
        }

        //---------------------------------------------------------------------

        private ModelListView listView;
        private Form form;

        [SetUp]
        public void SetUp()
        {
            this.listView = new ModelListView();
            this.listView.Columns.Add(new ColumnHeader()
            {
                DisplayIndex = 0
            });
            this.listView.View = View.Details;

            this.form = new Form();
            this.form.Controls.Add(this.listView);

            this.form.Show();
        }

        [TearDown]
        public void TearDown()
        {
            this.form.Close();
        }

        //---------------------------------------------------------------------
        // Loading.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNoOtherPropertiesBound_ThenBindCollectionSucceeds()
        {
            var items = new ObservableCollection<ModelItem>();
            items.Add(new ModelItem());

            this.listView.BindCollection(items);

            Assert.AreEqual(1, this.listView.Items.Count);
        }

        //---------------------------------------------------------------------
        // Binding.
        //---------------------------------------------------------------------

        [Test]
        public void WhenModelPropertyIsUpdated_ThenColumnIsUpdated()
        {
            var item = new ModelItem()
            {
                Name = "initial name"
            };

            var items = new ObservableCollection<ModelItem>();
            items.Add(item);

            this.listView.BindColumn(0, m => m.Name);
            this.listView.BindCollection(items);

            Assert.AreEqual("initial name", this.listView.Items[0].SubItems[0].Text);

            item.Name = "new name";

            Assert.AreEqual("new name", this.listView.Items[0].SubItems[0].Text);
        }

        [Test]
        public void WhenModelAddsItems_ThenListViewIsUpdated()
        {
            var items = new ObservableCollection<ModelItem>();
            items.Add(new ModelItem()
            {
                Name = "one"
            });

            this.listView.BindColumn(0, m => m.Name);
            this.listView.BindCollection(items);

            Assert.AreEqual(1, this.listView.Items.Count);

            items.Add(new ModelItem()
            {
                Name = "two"
            });

            Assert.AreEqual(2, this.listView.Items.Count);
        }

        [Test]
        public void WhenModelRemovesItems_ThenListViewIsUpdated()
        {
            var items = new ObservableCollection<ModelItem>();
            items.Add(new ModelItem()
            {
                Name = "one"
            });
            items.Add(new ModelItem()
            {
                Name = "two"
            });

            this.listView.BindColumn(0, m => m.Name);
            this.listView.BindCollection(items);

            Assert.AreEqual(2, this.listView.Items.Count);

            items.RemoveAt(0);

            Assert.AreEqual(1, this.listView.Items.Count);
        }

        [Test]
        public void WhenModelReplacesItem_ThenListViewIsUpdated()
        {
            var items = new ObservableCollection<ModelItem>();
            items.Add(new ModelItem()
            {
                Name = "one"
            });
            items.Add(new ModelItem()
            {
                Name = "two"
            });

            this.listView.BindColumn(0, m => m.Name);
            this.listView.BindCollection(items);

            Assert.AreEqual(2, this.listView.Items.Count);
            Assert.AreEqual("one", this.listView.Items[0].SubItems[0].Text);

            items[0] = new ModelItem()
            {
                Name = "three"
            };

            Assert.AreEqual(2, this.listView.Items.Count);
            Assert.AreEqual("three", this.listView.Items[0].SubItems[0].Text);
        }

        [Test]
        public void WhenModelRemovesItem_ThenEventListenersAreRemoved()
        {
            var item = new ModelItem()
            {
                Name = "initial name"
            };

            var items = new ObservableCollection<ModelItem>();
            items.Add(item);

            this.listView.BindColumn(0, m => m.Name);
            this.listView.BindCollection(items);

            Assert.IsTrue(item.HasPropertyChangeListeners);

            items.RemoveAt(0);

            Assert.IsFalse(item.HasPropertyChangeListeners);
        }

        [Test]
        public void WhenModelCleared_ThenEventListenersAreRemoved()
        {
            var item = new ModelItem()
            {
                Name = "initial name"
            };

            var items = new ObservableCollection<ModelItem>();
            items.Add(item);

            this.listView.BindColumn(0, m => m.Name);
            this.listView.BindCollection(items);

            Assert.IsTrue(item.HasPropertyChangeListeners);

            items.Clear();

            Assert.IsFalse(item.HasPropertyChangeListeners);
        }

        [Test]
        public void WhenModelIsRebound_ThenEventListenersAreRemoved()
        {
            var item = new ModelItem()
            {
                Name = "initial name"
            };

            var items = new ObservableCollection<ModelItem>();
            items.Add(item);

            this.listView.BindColumn(0, m => m.Name);
            this.listView.BindCollection(items);

            Assert.IsTrue(item.HasPropertyChangeListeners);

            this.listView.BindCollection(new ObservableCollection<ModelItem>());

            Assert.IsFalse(item.HasPropertyChangeListeners);
        }

        [Test]
        public void WhenModelChangesImageIndex_ThenControlIsUpdated()
        {
            var item1 = new ModelItem()
            {
                Name = "one",
                ImageIndex = 1
            };
            var item2 = new ModelItem()
            {
                Name = "two",
                ImageIndex = 2
            };

            var items = new ObservableCollection<ModelItem>();
            items.Add(item1);
            items.Add(item2);

            this.listView.BindColumn(0, m => m.Name);
            this.listView.BindImageIndex(m => m.ImageIndex);
            this.listView.BindCollection(items);

            Assert.AreEqual(1, this.listView.Items[0].ImageIndex);
            Assert.AreEqual(2, this.listView.Items[1].ImageIndex);

            item1.ImageIndex = 0;
            Assert.AreEqual(0, this.listView.Items[0].ImageIndex);
            Assert.AreEqual(2, this.listView.Items[1].ImageIndex);
        }
    }
}
