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
            private string? name;
            private bool expanded = false;

            public ObservableCollection<ModelNode> Children = new ObservableCollection<ModelNode>();

            public string Name
            {
                get => this.name!;
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

#pragma warning disable CS8618 // Non-nullable field 
        private ModelTreeView tree;
        private Form form;
#pragma warning restore CS8618 

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

            Assert.That(this.tree.Nodes.Count, Is.EqualTo(1));
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

            Assert.That(this.tree.Nodes.Count, Is.EqualTo(1));
            var rootTreeNode = this.tree.Nodes.OfType<ModelTreeView.Node>().First();
            Assert.That(rootTreeNode.Nodes.Count, Is.EqualTo(2));
            Assert.That(rootTreeNode.IsExpanded, Is.True);
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

            Assert.That(this.tree.Nodes.Count, Is.EqualTo(1));
            var rootTreeNode = this.tree.Nodes.OfType<ModelTreeView.Node>().First();
            Assert.That(rootTreeNode.Nodes.Count, Is.EqualTo(1));   // Loading node, hidden
            Assert.That(rootTreeNode.IsExpanded, Is.False);
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

            Assert.That(eventCount, Is.EqualTo(1));

            var rootTreeNode = this.tree.Nodes.OfType<ModelTreeView.Node>().First();
            Assert.That(rootTreeNode.IsExpanded, Is.False);
            Assert.That(root.IsExpanded, Is.False);

            // Try again.
            rootTreeNode.Expand();
            RunPendingAsyncTasks();
            Assert.That(eventCount, Is.EqualTo(2));

            // Try again, but this time let loading succeed.
            this.tree.BindChildren(m => m.GetChildren());
            rootTreeNode.Expand();
            RunPendingAsyncTasks();
            Assert.That(eventCount, Is.EqualTo(2));
            Assert.That(rootTreeNode.IsExpanded, Is.True);
            Assert.That(root.IsExpanded, Is.True);
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

            Assert.That(this.tree.Nodes.Count, Is.EqualTo(1));
            var rootTreeNode = this.tree.Nodes.OfType<ModelTreeView.Node>().First();
            Assert.That(rootTreeNode.Text, Is.EqualTo("root"));
            Assert.That(rootTreeNode.Name, Is.EqualTo("root"));

            root.Name = "new";
            Assert.That(rootTreeNode.Text, Is.EqualTo("new"));
            Assert.That(rootTreeNode.Name, Is.EqualTo("root"));
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

            Assert.That(this.tree.Nodes.Count, Is.EqualTo(1));
            var rootTreeNode = this.tree.Nodes.OfType<ModelTreeView.Node>().First();
            Assert.That(rootTreeNode.ImageIndex, Is.EqualTo(1));
            Assert.That(rootTreeNode.SelectedImageIndex, Is.EqualTo(1));

            root.ImageIndex = 2;
            Assert.That(rootTreeNode.ImageIndex, Is.EqualTo(2));
            Assert.That(rootTreeNode.SelectedImageIndex, Is.EqualTo(2));
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
            Assert.That(rootTreeNode.Nodes.Count, Is.EqualTo(2));

            root.Children.Add(new ModelNode()
            {
                Name = "child-3"
            });

            Assert.That(rootTreeNode.Nodes.Count, Is.EqualTo(3));
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
            Assert.That(rootTreeNode.Nodes.Count, Is.EqualTo(2));

            root.Children.RemoveAt(0);

            Assert.That(rootTreeNode.Nodes.Count, Is.EqualTo(1));
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
            Assert.That(rootTreeNode.Nodes.Count, Is.EqualTo(2));

            root.Children[0] = new ModelNode()
            {
                Name = "new"
            };

            Assert.That(rootTreeNode.Nodes.Count, Is.EqualTo(2));
            Assert.That(rootTreeNode.Nodes.OfType<ModelTreeView.Node>().First().Model.Name, Is.EqualTo("new"));
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
            Assert.That(child.HasPropertyChangeListeners, Is.False);

            root.Children.Add(child);

            this.tree.BindChildren(m => m.GetChildren());
            this.tree.BindIsExpanded(m => m.IsExpanded);
            this.tree.Bind(root, new Mock<IBindingContext>().Object);
            RunPendingAsyncTasks();

            Assert.That(child.HasPropertyChangeListeners, Is.True);
            root.Children.RemoveAt(0);
            Assert.That(child.HasPropertyChangeListeners, Is.False);
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

            Assert.That(root.HasPropertyChangeListeners, Is.True);

            var newRoot = new ModelNode()
            {
                Name = "root2"
            };

            this.tree.Bind(
                newRoot,
                new Mock<IBindingContext>().Object);

            Assert.That(root.HasPropertyChangeListeners, Is.False);
            Assert.That(newRoot.HasPropertyChangeListeners, Is.True);
        }
    }
}
