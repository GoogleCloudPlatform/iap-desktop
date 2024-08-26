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

using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestBindableTreeView
    {
        private class ModelNode : ViewModelBase
        {
            private int imageIndex;
            private string name;
            private bool expanded = false;

            public ObservableCollection<ModelNode> Children = new ObservableCollection<ModelNode>();

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

            public bool IsExpanded
            {
                get => this.expanded;
                set
                {
                    this.expanded = value;
                    RaisePropertyChange();
                }
            }

            public Task<ObservableCollection<ModelNode>> GetChildren()
            {
                return Task.FromResult(this.Children);
            }

            public Task<ObservableCollection<ModelNode>> Throw()
            {
                return Task.FromException<ObservableCollection<ModelNode>>(
                    new ArgumentException());
            }
        }

        private class ModelTreeView : BindableTreeView<ModelNode>
        {
        }

        //---------------------------------------------------------------------

        private ModelTreeView tree;
        private Form form;

        [SetUp]
        public void SetUp()
        {
            this.tree = new ModelTreeView();

            this.form = new Form();
            this.form.Controls.Add(this.tree);

            this.form.Show();
        }

        [TearDown]
        public void TearDown()
        {
            this.form.Close();
        }

        private void RunPendingAsyncTasks() => System.Windows.Forms.Application.DoEvents();

        //---------------------------------------------------------------------
        // Loading.
        //---------------------------------------------------------------------

        [Test]
        public void Bind_WhenNoOtherPropertiesBound_ThenBindSucceeds()
        {
            this.tree.Bind(
                new ModelNode(),
                new Mock<IBindingContext>().Object);

            Assert.AreEqual(1, this.tree.Nodes.Count);
        }

        [Test]
        public void Bind_WhenIsExpandedIsTrueInModel_ThenTreeAutoExpands()
        {
            var root = new ModelNode()
            {
                Name = "root",
                IsExpanded = true
            };
            root.Children.Add(new ModelNode()
            {
                Name = "child-1"
            });
            root.Children.Add(new ModelNode()
            {
                Name = "child-2"
            });

            this.tree.BindChildren(m => m.GetChildren());
            this.tree.BindIsExpanded(m => m.IsExpanded);
            this.tree.Bind(root, new Mock<IBindingContext>().Object);
            RunPendingAsyncTasks();

            Assert.AreEqual(1, this.tree.Nodes.Count);
            var rootTreeNode = this.tree.Nodes.OfType<ModelTreeView.Node>().First();
            Assert.AreEqual(2, rootTreeNode.Nodes.Count);
            Assert.IsTrue(rootTreeNode.IsExpanded);
        }

        [Test]
        public void Bind_WhenIsExpandedIsFalseInModel_ThenTreeHasHiddenLoadingNode()
        {
            var root = new ModelNode()
            {
                Name = "root",
                IsExpanded = false
            };
            root.Children.Add(new ModelNode()
            {
                Name = "child-1"
            });
            root.Children.Add(new ModelNode()
            {
                Name = "child-2"
            });

            this.tree.BindChildren(m => m.GetChildren());
            this.tree.BindIsExpanded(m => m.IsExpanded);
            this.tree.Bind(root, new Mock<IBindingContext>().Object);
            RunPendingAsyncTasks();

            Assert.AreEqual(1, this.tree.Nodes.Count);
            var rootTreeNode = this.tree.Nodes.OfType<ModelTreeView.Node>().First();
            Assert.AreEqual(1, rootTreeNode.Nodes.Count);   // Loading node, hidden
            Assert.IsFalse(rootTreeNode.IsExpanded);
        }

        [Test]
        public void Bind_WhenLoadingChildrenFails_ThenEventIsFiredAndExpandCanBeRetried()
        {
            var root = new ModelNode()
            {
                Name = "root",
                IsExpanded = true
            };

            var eventCount = 0;
            this.tree.LoadingChildrenFailed += (sender, args) =>
            {
                eventCount++;
                Assert.IsInstanceOf<ModelNode>(sender);
                Assert.IsInstanceOf<ArgumentException>(args.Exception.Unwrap());
            };

            this.tree.BindChildren(m => m.Throw());
            this.tree.BindIsExpanded(m => m.IsExpanded);
            this.tree.Bind(root, new Mock<IBindingContext>().Object);
            RunPendingAsyncTasks();

            Assert.AreEqual(1, eventCount);

            var rootTreeNode = this.tree.Nodes.OfType<ModelTreeView.Node>().First();
            Assert.IsFalse(rootTreeNode.IsExpanded);
            Assert.IsFalse(root.IsExpanded);

            // Try again.
            rootTreeNode.Expand();
            RunPendingAsyncTasks();
            Assert.AreEqual(2, eventCount);

            // Try again, but this time let loading succeed.
            this.tree.BindChildren(m => m.GetChildren());
            rootTreeNode.Expand();
            RunPendingAsyncTasks();
            Assert.AreEqual(2, eventCount);
            Assert.IsTrue(rootTreeNode.IsExpanded);
            Assert.IsTrue(root.IsExpanded);
        }

        //---------------------------------------------------------------------
        // Binding.
        //---------------------------------------------------------------------

        [Test]
        public void Bind_WhenModelTextIsUpdated_ThenTreeViewIsUpdated()
        {
            var root = new ModelNode()
            {
                Name = "root"
            };

            this.tree.BindText(m => m.Name);
            this.tree.Bind(root, new Mock<IBindingContext>().Object);

            Assert.AreEqual(1, this.tree.Nodes.Count);
            var rootTreeNode = this.tree.Nodes.OfType<ModelTreeView.Node>().First();
            Assert.AreEqual("root", rootTreeNode.Text);
            Assert.AreEqual("root", rootTreeNode.Name);

            root.Name = "new";
            Assert.AreEqual("new", rootTreeNode.Text);
            Assert.AreEqual("root", rootTreeNode.Name);
        }

        [Test]
        public void Bind_WhenModelImageIndexIsUpdated_ThenTreeViewIsUpdated()
        {
            var root = new ModelNode()
            {
                ImageIndex = 1
            };

            this.tree.BindImageIndex(m => m.ImageIndex);
            this.tree.BindSelectedImageIndex(m => m.ImageIndex);
            this.tree.Bind(root, new Mock<IBindingContext>().Object);

            Assert.AreEqual(1, this.tree.Nodes.Count);
            var rootTreeNode = this.tree.Nodes.OfType<ModelTreeView.Node>().First();
            Assert.AreEqual(1, rootTreeNode.ImageIndex);
            Assert.AreEqual(1, rootTreeNode.SelectedImageIndex);

            root.ImageIndex = 2;
            Assert.AreEqual(2, rootTreeNode.ImageIndex);
            Assert.AreEqual(2, rootTreeNode.SelectedImageIndex);
        }

        [Test]
        public void Bind_WhenModelAddsChildren_ThenTreeViewIsUpdated()
        {
            var root = new ModelNode()
            {
                Name = "root",
                IsExpanded = true
            };
            root.Children.Add(new ModelNode()
            {
                Name = "child-1"
            });
            root.Children.Add(new ModelNode()
            {
                Name = "child-2"
            });

            this.tree.BindChildren(m => m.GetChildren());
            this.tree.BindIsExpanded(m => m.IsExpanded);
            this.tree.Bind(root, new Mock<IBindingContext>().Object);
            RunPendingAsyncTasks();

            var rootTreeNode = this.tree.Nodes.OfType<ModelTreeView.Node>().First();
            Assert.AreEqual(2, rootTreeNode.Nodes.Count);

            root.Children.Add(new ModelNode()
            {
                Name = "child-3"
            });

            Assert.AreEqual(3, rootTreeNode.Nodes.Count);
        }

        [Test]
        public void Bind_WhenModelRemovesChildren_ThenTreeViewIsUpdated()
        {
            var root = new ModelNode()
            {
                Name = "root",
                IsExpanded = true
            };
            root.Children.Add(new ModelNode()
            {
                Name = "child-1"
            });
            root.Children.Add(new ModelNode()
            {
                Name = "child-2"
            });

            this.tree.BindChildren(m => m.GetChildren());
            this.tree.BindIsExpanded(m => m.IsExpanded);
            this.tree.Bind(root, new Mock<IBindingContext>().Object);
            RunPendingAsyncTasks();

            var rootTreeNode = this.tree.Nodes.OfType<ModelTreeView.Node>().First();
            Assert.AreEqual(2, rootTreeNode.Nodes.Count);

            root.Children.RemoveAt(0);

            Assert.AreEqual(1, rootTreeNode.Nodes.Count);
        }

        [Test]
        public void Bind_WhenModelReplacesChildren_ThenTreeViewIsUpdated()
        {
            var root = new ModelNode()
            {
                Name = "root",
                IsExpanded = true
            };
            root.Children.Add(new ModelNode()
            {
                Name = "child-1"
            });
            root.Children.Add(new ModelNode()
            {
                Name = "child-2"
            });

            this.tree.BindChildren(m => m.GetChildren());
            this.tree.BindIsExpanded(m => m.IsExpanded);
            this.tree.Bind(root, new Mock<IBindingContext>().Object);
            RunPendingAsyncTasks();

            var rootTreeNode = this.tree.Nodes.OfType<ModelTreeView.Node>().First();
            Assert.AreEqual(2, rootTreeNode.Nodes.Count);

            root.Children[0] = new ModelNode()
            {
                Name = "new"
            };

            Assert.AreEqual(2, rootTreeNode.Nodes.Count);
            Assert.AreEqual("new", rootTreeNode.Nodes.OfType<ModelTreeView.Node>().First().Model.Name);
        }

        [Test]
        public void Bind_WhenModelRemovesChild_ThenEventListenersAreRemoved()
        {
            var root = new ModelNode()
            {
                Name = "root",
                IsExpanded = true
            };

            var child = new ModelNode()
            {
                Name = "child-1"
            };
            Assert.IsFalse(child.HasPropertyChangeListeners);

            root.Children.Add(child);

            this.tree.BindChildren(m => m.GetChildren());
            this.tree.BindIsExpanded(m => m.IsExpanded);
            this.tree.Bind(root, new Mock<IBindingContext>().Object);
            RunPendingAsyncTasks();

            Assert.IsTrue(child.HasPropertyChangeListeners);
            root.Children.RemoveAt(0);
            Assert.IsFalse(child.HasPropertyChangeListeners);
        }

        [Test]
        public void Bind_WhenModelIsRebound_ThenEventListenersAreRemoved()
        {
            var root = new ModelNode()
            {
                Name = "root"
            };

            this.tree.BindChildren(m => m.GetChildren());
            this.tree.BindIsExpanded(m => m.IsExpanded);
            this.tree.Bind(root, new Mock<IBindingContext>().Object);
            RunPendingAsyncTasks();

            Assert.IsTrue(root.HasPropertyChangeListeners);

            var newRoot = new ModelNode()
            {
                Name = "root2"
            };

            this.tree.Bind(
                newRoot,
                new Mock<IBindingContext>().Object);

            Assert.IsFalse(root.HasPropertyChangeListeners);
            Assert.IsTrue(newRoot.HasPropertyChangeListeners);
        }
    }
}
