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

using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestBindableListView
    {
        private class ViewModelItem : ViewModelBase
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

        private class ViewModel : ViewModelBase
        {
            private ViewModelItem selectedItem = null;
            private IEnumerable<ViewModelItem> selectedItems = null;

            public ObservableCollection<ViewModelItem> Items { get; set; }

            public ViewModelItem SelectedItem
            {
                get => this.selectedItem;
                set
                {
                    this.selectedItem = value;
                    RaisePropertyChange();
                }
            }
            public IEnumerable<ViewModelItem> SelectedItems
            {
                get => this.selectedItems;
                set
                {
                    this.selectedItems = value;
                    RaisePropertyChange();
                }
            }
        }

        private class ModelListView : BindableListView<ViewModelItem>
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
        public void BindCollection_WhenNoOtherPropertiesBound_ThenBindCollectionSucceeds()
        {
            var items = new ObservableCollection<ViewModelItem>
            {
                new ViewModelItem()
            };

            this.listView.BindCollection(items);

            Assert.AreEqual(1, this.listView.Items.Count);
        }

        //---------------------------------------------------------------------
        // BindCollection.
        //---------------------------------------------------------------------

        [Test]
        public void BindCollection_WhenModelPropertyIsUpdated_ThenColumnIsUpdated()
        {
            var item = new ViewModelItem()
            {
                Name = "initial name"
            };

            var items = new ObservableCollection<ViewModelItem>
            {
                item
            };

            this.listView.BindColumn(0, m => m.Name);
            this.listView.BindCollection(items);

            Assert.AreEqual("initial name", this.listView.Items[0].SubItems[0].Text);

            item.Name = "new name";

            Assert.AreEqual("new name", this.listView.Items[0].SubItems[0].Text);
        }

        [Test]
        public void BindCollection_WhenModelAddsItems_ThenListViewIsUpdated()
        {
            var items = new ObservableCollection<ViewModelItem>
            {
                new ViewModelItem()
                {
                    Name = "one"
                }
            };

            this.listView.BindColumn(0, m => m.Name);
            this.listView.BindCollection(items);

            Assert.AreEqual(1, this.listView.Items.Count);

            items.Add(new ViewModelItem()
            {
                Name = "two"
            });

            Assert.AreEqual(2, this.listView.Items.Count);
        }

        [Test]
        public void BindCollection_WhenModelRemovesItems_ThenListViewIsUpdated()
        {
            var items = new ObservableCollection<ViewModelItem>
            {
                new ViewModelItem()
                {
                    Name = "one"
                },
                new ViewModelItem()
                {
                    Name = "two"
                }
            };

            this.listView.BindColumn(0, m => m.Name);
            this.listView.BindCollection(items);

            Assert.AreEqual(2, this.listView.Items.Count);

            items.RemoveAt(0);

            Assert.AreEqual(1, this.listView.Items.Count);
        }

        [Test]
        public void BindCollection_WhenModelReplacesItem_ThenListViewIsUpdated()
        {
            var items = new ObservableCollection<ViewModelItem>
            {
                new ViewModelItem()
                {
                    Name = "one"
                },
                new ViewModelItem()
                {
                    Name = "two"
                }
            };

            this.listView.BindColumn(0, m => m.Name);
            this.listView.BindCollection(items);

            Assert.AreEqual(2, this.listView.Items.Count);
            Assert.AreEqual("one", this.listView.Items[0].SubItems[0].Text);

            items[0] = new ViewModelItem()
            {
                Name = "three"
            };

            Assert.AreEqual(2, this.listView.Items.Count);
            Assert.AreEqual("three", this.listView.Items[0].SubItems[0].Text);
        }

        [Test]
        public void BindCollection_WhenModelRemovesItem_ThenEventListenersAreRemoved()
        {
            var item = new ViewModelItem()
            {
                Name = "initial name"
            };

            var items = new ObservableCollection<ViewModelItem>
            {
                item
            };

            this.listView.BindColumn(0, m => m.Name);
            this.listView.BindCollection(items);

            Assert.IsTrue(item.HasPropertyChangeListeners);

            items.RemoveAt(0);

            Assert.IsFalse(item.HasPropertyChangeListeners);
        }

        [Test]
        public void BindCollection_WhenModelCleared_ThenEventListenersAreRemoved()
        {
            var item = new ViewModelItem()
            {
                Name = "initial name"
            };

            var items = new ObservableCollection<ViewModelItem>
            {
                item
            };

            this.listView.BindColumn(0, m => m.Name);
            this.listView.BindCollection(items);

            Assert.IsTrue(item.HasPropertyChangeListeners);

            items.Clear();

            Assert.IsFalse(item.HasPropertyChangeListeners);
        }

        [Test]
        public void BindCollection_WhenModelIsRebound_ThenEventListenersAreRemoved()
        {
            var item = new ViewModelItem()
            {
                Name = "initial name"
            };

            var items = new ObservableCollection<ViewModelItem>
            {
                item
            };

            this.listView.BindColumn(0, m => m.Name);
            this.listView.BindCollection(items);

            Assert.IsTrue(item.HasPropertyChangeListeners);

            this.listView.BindCollection(new ObservableCollection<ViewModelItem>());

            Assert.IsFalse(item.HasPropertyChangeListeners);
        }

        [Test]
        public void BindCollection_WhenModelChangesImageIndex_ThenControlIsUpdated()
        {
            var item1 = new ViewModelItem()
            {
                Name = "one",
                ImageIndex = 1
            };
            var item2 = new ViewModelItem()
            {
                Name = "two",
                ImageIndex = 2
            };

            var items = new ObservableCollection<ViewModelItem>
            {
                item1,
                item2
            };

            this.listView.BindColumn(0, m => m.Name);
            this.listView.BindImageIndex(m => m.ImageIndex);
            this.listView.BindCollection(items);

            Assert.AreEqual(1, this.listView.Items[0].ImageIndex);
            Assert.AreEqual(2, this.listView.Items[1].ImageIndex);

            item1.ImageIndex = 0;
            Assert.AreEqual(0, this.listView.Items[0].ImageIndex);
            Assert.AreEqual(2, this.listView.Items[1].ImageIndex);
        }

        //---------------------------------------------------------------------
        // SelectedModelItem.
        //---------------------------------------------------------------------

        [Test]
        public void SelectedModelItem_WhenControlChangesSelectedItem_ThenModelIsUpdated()
        {
            var viewModel = new ViewModel()
            {
                Items = new ObservableCollection<ViewModelItem>
                {
                    new ViewModelItem()
                    {
                        Name = "one",
                        ImageIndex = 1
                    },
                    new ViewModelItem()
                    {
                        Name = "two",
                        ImageIndex = 2
                    }
                }
            };

            this.listView.BindColumn(0, m => m.Name);
            this.listView.BindImageIndex(m => m.ImageIndex);
            this.listView.BindCollection(viewModel.Items);
            this.listView.BindProperty(
                c => c.SelectedModelItem,
                viewModel,
                m => m.SelectedItem,
                new Mock<IBindingContext>().Object);

            this.listView.Items[0].Selected = true;

            Assert.IsNotNull(viewModel.SelectedItem);
            Assert.AreEqual("one", viewModel.SelectedItem.Name);
        }

        [Test]
        public void SelectedModelItem_WhenModelChangesSelectedItem_ThenControlIsUpdated()
        {
            var viewModel = new ViewModel()
            {
                Items = new ObservableCollection<ViewModelItem>
                {
                    new ViewModelItem()
                    {
                        Name = "one",
                        ImageIndex = 1
                    },
                    new ViewModelItem()
                    {
                        Name = "two",
                        ImageIndex = 2
                    }
                }
            };

            this.listView.BindColumn(0, m => m.Name);
            this.listView.BindImageIndex(m => m.ImageIndex);
            this.listView.BindCollection(viewModel.Items);
            this.listView.BindProperty(
                c => c.SelectedModelItem,
                viewModel,
                m => m.SelectedItem,
                new Mock<IBindingContext>().Object);

            Assert.IsFalse(this.listView.Items[0].Selected);

            viewModel.SelectedItem = viewModel.Items[0];

            Assert.IsTrue(this.listView.Items[0].Selected);
            Assert.IsFalse(this.listView.Items[1].Selected);
        }

        //---------------------------------------------------------------------
        // SelectedModelItems.
        //---------------------------------------------------------------------

        [Test]
        public void SelectedModelItems_WhenControlChangesSelectedItems_ThenModelIsUpdated()
        {
            var viewModel = new ViewModel()
            {
                Items = new ObservableCollection<ViewModelItem>
                {
                    new ViewModelItem()
                    {
                        Name = "one",
                        ImageIndex = 1
                    },
                    new ViewModelItem()
                    {
                        Name = "two",
                        ImageIndex = 2
                    }
                }
            };

            this.listView.BindColumn(0, m => m.Name);
            this.listView.BindImageIndex(m => m.ImageIndex);
            this.listView.BindCollection(viewModel.Items);
            this.listView.BindProperty(
                c => c.SelectedModelItems,
                viewModel,
                m => m.SelectedItems,
                new Mock<IBindingContext>().Object);

            this.listView.Items[0].Selected = true;
            this.listView.Items[1].Selected = true;

            Assert.IsNotNull(viewModel.SelectedItems);
            Assert.AreEqual(2, viewModel.SelectedItems.Count());
        }

        [Test]
        public void SelectedModelItems_WhenModelChangesSelectedItems_ThenControlIsUpdated()
        {
            var viewModel = new ViewModel()
            {
                Items = new ObservableCollection<ViewModelItem>
                {
                    new ViewModelItem()
                    {
                        Name = "one",
                        ImageIndex = 1
                    },
                    new ViewModelItem()
                    {
                        Name = "two",
                        ImageIndex = 2
                    }
                }
            };

            this.listView.BindColumn(0, m => m.Name);
            this.listView.BindImageIndex(m => m.ImageIndex);
            this.listView.BindCollection(viewModel.Items);
            this.listView.BindProperty(
                c => c.SelectedModelItems,
                viewModel,
                m => m.SelectedItems,
                new Mock<IBindingContext>().Object);

            Assert.IsFalse(this.listView.Items[0].Selected);

            viewModel.SelectedItems = viewModel.Items;

            Assert.IsTrue(this.listView.Items[0].Selected);
            Assert.IsTrue(this.listView.Items[1].Selected);
        }
    }
}
