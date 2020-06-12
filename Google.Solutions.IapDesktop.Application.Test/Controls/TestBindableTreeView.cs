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
using Google.Solutions.IapDesktop.Application.Controls;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using NUnit.Framework;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestBindableTreeView : FixtureBase
    {
        private class ModelNode : ViewModelBase
        {
            private int imageIndex;
            private string name;
            private bool expanded = false;

            public bool IsLeaf => false;

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
        public void WhenNoOtherPropertiesBound_ThenBindSucceeds()
        {
            tree.Bind(new ModelNode());

            Assert.AreEqual(1, this.tree.Nodes.Count);
        }

        [Test]
        public void WhenIsExpandedIsTrueInModel_ThenTreeAutoExpands()
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

            tree.BindChildren(m => m.GetChildren());
            tree.BindIsExpanded(m => m.IsExpanded);
            tree.Bind(root);
            RunPendingAsyncTasks();

            Assert.AreEqual(1, this.tree.Nodes.Count);
            var rootTreeNode = this.tree.Nodes.OfType<ModelTreeView.Node>().First();
            Assert.AreEqual(2, rootTreeNode.Nodes.Count);
            Assert.IsTrue(rootTreeNode.IsExpanded);
        }

        [Test]
        public void WhenIsExpandedIsFalseInModel_ThenTreeHasHiddenLoadingNode()
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

            tree.BindChildren(m => m.GetChildren());
            tree.BindIsExpanded(m => m.IsExpanded);
            tree.Bind(root);
            RunPendingAsyncTasks();

            Assert.AreEqual(1, this.tree.Nodes.Count);
            var rootTreeNode = this.tree.Nodes.OfType<ModelTreeView.Node>().First();
            Assert.AreEqual(1, rootTreeNode.Nodes.Count);   // Loading node, hidden
            Assert.IsFalse(rootTreeNode.IsExpanded);
        }

        [Test]
        public void WhenLoadingChildrenFails_ThenEventIsFiredAndExpandCanBeRetried()
        {
            var root = new ModelNode()
            {
                Name = "root",
                IsExpanded = true
            };

            int eventCount = 0;
            tree.LoadingChildrenFailed += (sender, args) =>
            {
                eventCount++;
                Assert.IsInstanceOf<ModelNode>(sender);
                Assert.IsInstanceOf<ArgumentException>(args.Exception.Unwrap());
            };

            tree.BindChildren(m => m.Throw());
            tree.BindIsExpanded(m => m.IsExpanded);
            tree.Bind(root);
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
            tree.BindChildren(m => m.GetChildren());
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
        public void WhenModelTextIsUpdated_ThenTreeViewIsUpdated()
        {
            var root = new ModelNode()
            {
                Name = "root"
            };

            tree.BindText(m => m.Name);
            tree.Bind(root);

            Assert.AreEqual(1, this.tree.Nodes.Count);
            var rootTreeNode = this.tree.Nodes.OfType<ModelTreeView.Node>().First();
            Assert.AreEqual("root", rootTreeNode.Text);
            Assert.AreEqual("root", rootTreeNode.Name);

            root.Name = "new";
            Assert.AreEqual("new", rootTreeNode.Text);
            Assert.AreEqual("root", rootTreeNode.Name);
        }

        [Test]
        public void WhenModelImageIndexIsUpdated_ThenTreeViewIsUpdated()
        {
            var root = new ModelNode()
            {
                ImageIndex = 1
            };

            tree.BindImageIndex(m => m.ImageIndex);
            tree.BindSelectedImageIndex(m => m.ImageIndex);
            tree.Bind(root);

            Assert.AreEqual(1, this.tree.Nodes.Count);
            var rootTreeNode = this.tree.Nodes.OfType<ModelTreeView.Node>().First();
            Assert.AreEqual(1, rootTreeNode.ImageIndex);
            Assert.AreEqual(1, rootTreeNode.SelectedImageIndex);

            root.ImageIndex = 2;
            Assert.AreEqual(2, rootTreeNode.ImageIndex);
            Assert.AreEqual(2, rootTreeNode.SelectedImageIndex);
        }

        [Test]
        public void WhenModelAddsChildren_ThenTreeViewIsUpdated()
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

            tree.BindChildren(m => m.GetChildren());
            tree.BindIsExpanded(m => m.IsExpanded);
            tree.Bind(root);
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
        public void WhenModelRemovesChildren_ThenTreeViewIsUpdated()
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

            tree.BindChildren(m => m.GetChildren());
            tree.BindIsExpanded(m => m.IsExpanded);
            tree.Bind(root);
            RunPendingAsyncTasks();

            var rootTreeNode = this.tree.Nodes.OfType<ModelTreeView.Node>().First();
            Assert.AreEqual(2, rootTreeNode.Nodes.Count);

            root.Children.RemoveAt(0);

            Assert.AreEqual(1, rootTreeNode.Nodes.Count);
        }

        [Test]
        public void WhenModelReplacesChildren_ThenTreeViewIsUpdated()
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

            tree.BindChildren(m => m.GetChildren());
            tree.BindIsExpanded(m => m.IsExpanded);
            tree.Bind(root);
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
        public void WhenModelRemovesChild_ThenEventListenersAreRemoved()
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

            tree.BindChildren(m => m.GetChildren());
            tree.BindIsExpanded(m => m.IsExpanded);
            tree.Bind(root);
            RunPendingAsyncTasks();

            Assert.IsTrue(child.HasPropertyChangeListeners);
            root.Children.RemoveAt(0);
            Assert.IsFalse(child.HasPropertyChangeListeners);
        }

        [Test]
        public void WhenModelIsRebound_ThenEventListenersAreRemoved()
        {
            var root = new ModelNode()
            {
                Name = "root"
            };

            tree.BindChildren(m => m.GetChildren());
            tree.BindIsExpanded(m => m.IsExpanded);
            tree.Bind(root);
            RunPendingAsyncTasks();

            Assert.IsTrue(root.HasPropertyChangeListeners);

            var newRoot = new ModelNode()
            {
                Name = "root2"
            };

            tree.Bind(newRoot);

            Assert.IsFalse(root.HasPropertyChangeListeners);
            Assert.IsTrue(newRoot.HasPropertyChangeListeners);
        }
    }
}
