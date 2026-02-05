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

using Google.Solutions.Common.Linq;
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

            public ViewModelItem(string name, int imageIndex = 0)
            {
                this.name = name;
                this.imageIndex = imageIndex;
            }

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
            private ViewModelItem? selectedItem = null;
            private IEnumerable<ViewModelItem>? selectedItems = null;

            public ObservableCollection<ViewModelItem> Items { get; }

            public ViewModel(ObservableCollection<ViewModelItem> items)
            {
                this.Items = items;
            }

            public ViewModelItem? SelectedItem
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
                get => this.selectedItems.EnsureNotNull();
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

        private class TestForm : Form
        {
            public ModelListView ListView { get; }

            public TestForm()
            {
                this.ListView = new ModelListView();
                this.ListView.Columns.Add(new ColumnHeader()
                {
                    DisplayIndex = 0
                });
                this.ListView.View = View.Details;

                this.Controls.Add(this.ListView);
            }
        }

        //---------------------------------------------------------------------
        // Loading.
        //---------------------------------------------------------------------

        [Test]
        public void BindCollection_WhenNoOtherPropertiesBound_ThenBindCollectionSucceeds()
        {
            var items = new ObservableCollection<ViewModelItem>
            {
                new ViewModelItem("one")
            };

            using (var form = new TestForm())
            {
                form.Show();
                form.ListView.BindCollection(items);

                Assert.That(form.ListView.Items.Count, Is.EqualTo(1));
            }
        }

        //---------------------------------------------------------------------
        // BindCollection.
        //---------------------------------------------------------------------

        [Test]
        public void BindCollection_WhenModelPropertyIsUpdated_ThenColumnIsUpdated()
        {
            var item = new ViewModelItem("initial name");
            var items = new ObservableCollection<ViewModelItem>
            {
                item
            };
            using (var form = new TestForm())
            {
                form.Show();
                form.ListView.BindColumn(0, m => m.Name);
                form.ListView.BindCollection(items);

                Assert.That(form.ListView.Items[0].SubItems[0].Text, Is.EqualTo("initial name"));

                item.Name = "new name";

                Assert.That(form.ListView.Items[0].SubItems[0].Text, Is.EqualTo("new name"));
            }
        }

        [Test]
        public void BindCollection_WhenModelAddsItems_ThenListViewIsUpdated()
        {
            var items = new ObservableCollection<ViewModelItem>
            {
                new ViewModelItem("one")
            };

            using (var form = new TestForm())
            {
                form.Show();
                form.ListView.BindColumn(0, m => m.Name);
                form.ListView.BindCollection(items);

                Assert.That(form.ListView.Items.Count, Is.EqualTo(1));

                items.Add(new ViewModelItem("two"));

                Assert.That(form.ListView.Items.Count, Is.EqualTo(2));
            }
        }

        [Test]
        public void BindCollection_WhenModelRemovesItems_ThenListViewIsUpdated()
        {
            var items = new ObservableCollection<ViewModelItem>
            {
                new ViewModelItem("one"),
                new ViewModelItem("two")
            };

            using (var form = new TestForm())
            {
                form.Show();
                form.ListView.BindColumn(0, m => m.Name);
                form.ListView.BindCollection(items);

                Assert.That(form.ListView.Items.Count, Is.EqualTo(2));

                items.RemoveAt(0);

                Assert.That(form.ListView.Items.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void BindCollection_WhenModelReplacesItem_ThenListViewIsUpdated()
        {
            var items = new ObservableCollection<ViewModelItem>
            {
                new ViewModelItem("one"),
                new ViewModelItem("two")
            };

            using (var form = new TestForm())
            {
                form.Show();
                form.ListView.BindColumn(0, m => m.Name);
                form.ListView.BindCollection(items);

                Assert.That(form.ListView.Items.Count, Is.EqualTo(2));
                Assert.That(form.ListView.Items[0].SubItems[0].Text, Is.EqualTo("one"));

                items[0] = new ViewModelItem("three");

                Assert.That(form.ListView.Items.Count, Is.EqualTo(2));
                Assert.That(form.ListView.Items[0].SubItems[0].Text, Is.EqualTo("three"));
            }
        }

        [Test]
        public void BindCollection_WhenModelRemovesItem_ThenEventListenersAreRemoved()
        {
            var item = new ViewModelItem("initial name");
            var items = new ObservableCollection<ViewModelItem>
            {
                item
            };

            using (var form = new TestForm())
            {
                form.Show();
                form.ListView.BindColumn(0, m => m.Name);
                form.ListView.BindCollection(items);

                Assert.That(item.HasPropertyChangeListeners, Is.True);

                items.RemoveAt(0);

                Assert.That(item.HasPropertyChangeListeners, Is.False);
            }
        }

        [Test]
        public void BindCollection_WhenModelCleared_ThenEventListenersAreRemoved()
        {
            var item = new ViewModelItem("initial name");
            var items = new ObservableCollection<ViewModelItem>
            {
                item
            };

            using (var form = new TestForm())
            {
                form.Show();
                form.ListView.BindColumn(0, m => m.Name);
                form.ListView.BindCollection(items);

                Assert.That(item.HasPropertyChangeListeners, Is.True);

                items.Clear();

                Assert.That(item.HasPropertyChangeListeners, Is.False);
            }
        }

        [Test]
        public void BindCollection_WhenModelIsRebound_ThenEventListenersAreRemoved()
        {
            var item = new ViewModelItem("initial name");
            var items = new ObservableCollection<ViewModelItem>
            {
                item
            };

            using (var form = new TestForm())
            {
                form.Show();
                form.ListView.BindColumn(0, m => m.Name);
                form.ListView.BindCollection(items);

                Assert.That(item.HasPropertyChangeListeners, Is.True);

                form.ListView.BindCollection(new ObservableCollection<ViewModelItem>());

                Assert.That(item.HasPropertyChangeListeners, Is.False);
            }
        }

        [Test]
        public void BindCollection_WhenModelChangesImageIndex_ThenControlIsUpdated()
        {
            var item1 = new ViewModelItem("one", 1);
            var item2 = new ViewModelItem("two", 2);

            var items = new ObservableCollection<ViewModelItem>
            {
                item1,
                item2
            };

            using (var form = new TestForm())
            {
                form.Show();
                form.ListView.BindColumn(0, m => m.Name);
                form.ListView.BindImageIndex(m => m.ImageIndex);
                form.ListView.BindCollection(items);

                Assert.That(form.ListView.Items[0].ImageIndex, Is.EqualTo(1));
                Assert.That(form.ListView.Items[1].ImageIndex, Is.EqualTo(2));

                item1.ImageIndex = 0;
                Assert.That(form.ListView.Items[0].ImageIndex, Is.EqualTo(0));
                Assert.That(form.ListView.Items[1].ImageIndex, Is.EqualTo(2));
            }
        }

        //---------------------------------------------------------------------
        // SelectedModelItem.
        //---------------------------------------------------------------------

        [Test]
        public void SelectedModelItem_WhenControlChangesSelectedItem_ThenModelIsUpdated()
        {
            var viewModel = new ViewModel(
                new ObservableCollection<ViewModelItem>
                {
                    new ViewModelItem("one", 1),
                    new ViewModelItem("two", 2)
                });

            using (var form = new TestForm())
            {
                form.Show();
                form.ListView.BindColumn(0, m => m.Name);
                form.ListView.BindImageIndex(m => m.ImageIndex);
                form.ListView.BindCollection(viewModel.Items);
                form.ListView.BindProperty(
                    c => c.SelectedModelItem,
                    viewModel,
                    m => m.SelectedItem,
                    new Mock<IBindingContext>().Object);

                form.ListView.Items[0].Selected = true;

                Assert.IsNotNull(viewModel.SelectedItem);
                Assert.That(viewModel.SelectedItem!.Name, Is.EqualTo("one"));
            }
        }

        [Test]
        public void SelectedModelItem_WhenModelChangesSelectedItem_ThenControlIsUpdated()
        {
            var viewModel = new ViewModel(
                new ObservableCollection<ViewModelItem>
                {
                    new ViewModelItem("one", 1),
                    new ViewModelItem("two", 2)
                });

            using (var form = new TestForm())
            {
                form.Show();
                form.ListView.BindColumn(0, m => m.Name);
                form.ListView.BindImageIndex(m => m.ImageIndex);
                form.ListView.BindCollection(viewModel.Items);
                form.ListView.BindProperty(
                    c => c.SelectedModelItem,
                    viewModel,
                    m => m.SelectedItem,
                    new Mock<IBindingContext>().Object);

                Assert.That(form.ListView.Items[0].Selected, Is.False);

                viewModel.SelectedItem = viewModel.Items[0];

                Assert.That(form.ListView.Items[0].Selected, Is.True);
                Assert.That(form.ListView.Items[1].Selected, Is.False);
            }
        }

        //---------------------------------------------------------------------
        // SelectedModelItems.
        //---------------------------------------------------------------------

        [Test]
        public void SelectedModelItems_WhenControlChangesSelectedItems_ThenModelIsUpdated()
        {
            var viewModel = new ViewModel(
                new ObservableCollection<ViewModelItem>
                {
                    new ViewModelItem("one", 1),
                    new ViewModelItem("two", 2)
                });

            using (var form = new TestForm())
            {
                form.Show();
                form.ListView.BindColumn(0, m => m.Name);
                form.ListView.BindImageIndex(m => m.ImageIndex);
                form.ListView.BindCollection(viewModel.Items);
                form.ListView.BindProperty(
                    c => c.SelectedModelItems,
                    viewModel,
                    m => m.SelectedItems,
                    new Mock<IBindingContext>().Object);

                form.ListView.Items[0].Selected = true;
                form.ListView.Items[1].Selected = true;

                Assert.IsNotNull(viewModel.SelectedItems);
                Assert.That(viewModel.SelectedItems.Count(), Is.EqualTo(2));
            }
        }

        [Test]
        public void SelectedModelItems_WhenModelChangesSelectedItems_ThenControlIsUpdated()
        {
            var viewModel = new ViewModel(
                new ObservableCollection<ViewModelItem>
                {
                    new ViewModelItem("one", 1),
                    new ViewModelItem("two", 2)
                });

            using (var form = new TestForm())
            {
                form.Show();
                form.ListView.BindColumn(0, m => m.Name);
                form.ListView.BindImageIndex(m => m.ImageIndex);
                form.ListView.BindCollection(viewModel.Items);
                form.ListView.BindProperty(
                    c => c.SelectedModelItems,
                    viewModel,
                    m => m.SelectedItems,
                    new Mock<IBindingContext>().Object);

                Assert.That(form.ListView.Items[0].Selected, Is.False);

                viewModel.SelectedItems = viewModel.Items;

                Assert.That(form.ListView.Items[0].Selected, Is.True);
                Assert.That(form.ListView.Items[1].Selected, Is.True);
            }
        }
    }
}
