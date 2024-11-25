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

using Google.Solutions.Common.Runtime;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

#pragma warning disable CS1690 // Accessing a member on a field of a marshal-by-reference class may cause a runtime exception

namespace Google.Solutions.Mvvm.Controls
{
    public class BindableTreeView<TModelNode> : TreeView
        where TModelNode : class, INotifyPropertyChanged
    {
        private Expression<Func<TModelNode, bool>>? isExpandedExpression = null;
        private Expression<Func<TModelNode, string>>? textExpression = null;

        private Expression<Func<TModelNode, int>>? imageIndexExpression = null;
        private Expression<Func<TModelNode, int>>? selectedImageIndexExpression = null;

        private bool imageIndexExpressionReadonly = false;
        private bool selectedImageIndexExpressionReadonly = false;

        private Func<TModelNode, bool> isLeafFunc = _ => false;
        private Action<TModelNode, bool> setExpandedFunc = (n, state) => { };
        private Func<TModelNode, bool> isExpandedFunc = _ => false;
        private Func<TModelNode, Task<ICollection<TModelNode>>> getChildrenAsyncFunc
            = _ => Task.FromResult<ICollection<TModelNode>>(new ObservableCollection<TModelNode>());

        private readonly TaskScheduler taskScheduler;

        public event EventHandler? SelectedModelNodeChanged;
        public event EventHandler<ExceptionEventArgs>? LoadingChildrenFailed;

        public BindableTreeView()
        {
            this.taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

            this.BeforeExpand += (sender, args) => ((Node)args.Node).OnExpand();
            this.BeforeCollapse += (sender, args) => ((Node)args.Node).OnCollapse();

            // Consider right-click as a selection.
            this.NodeMouseClick += (sender, args) =>
            {
                if (args.Button == MouseButtons.Right)
                {
                    this.SelectedNode = args.Node;
                }
            };
            this.AfterSelect += (sender, args) =>
            {
                this.SelectedModelNodeChanged?.Invoke(this, EventArgs.Empty);
            };
        }

        public TModelNode? SelectedModelNode
        {
            get
            {
                if (this.SelectedNode is Node node)
                {
                    return node.Model;
                }
                else
                {
                    return null;
                }
            }
        }

        //---------------------------------------------------------------------
        // Data Binding.
        //---------------------------------------------------------------------

        private static void DisposeAndClear(TreeNodeCollection nodes)
        {
            var disposables = nodes.OfType<Node>().ToList();
            nodes.Clear();

            foreach (var node in disposables)
            {
                node.Dispose();
            }
        }

        public void Bind(TModelNode rootNode, IBindingContext bindingContext)
        {
            DisposeAndClear(this.Nodes);
            this.Nodes.Add(new Node(this, rootNode));
        }

        public void BindIsLeaf(Func<TModelNode, bool> isLeafFunc)
        {
            this.isLeafFunc = isLeafFunc;
        }

        public void BindImageIndex(
            Expression<Func<TModelNode, int>> imageIndexExpression,
            bool readOnly = false)
        {
            this.imageIndexExpression = imageIndexExpression;
            this.imageIndexExpressionReadonly = readOnly;
        }

        public void BindSelectedImageIndex(Expression<Func<TModelNode, int>>
            selectedImageIndexExpression,
            bool readOnly = false)
        {
            this.selectedImageIndexExpression = selectedImageIndexExpression;
            this.selectedImageIndexExpressionReadonly = readOnly;
        }

        public void BindText(Expression<Func<TModelNode, string>> nameExpression)
        {
            this.textExpression = nameExpression;
        }

        public void BindIsExpanded(Expression<Func<TModelNode, bool>> propertyExpression)
        {
            if (propertyExpression.Body is MemberExpression memberExpression &&
                memberExpression.Member is PropertyInfo propertyInfo)
            {
                this.isExpandedExpression = propertyExpression;
                this.isExpandedFunc = propertyExpression.Compile();
                this.setExpandedFunc = (node, value) => propertyInfo.SetValue(node, value);
            }
            else
            {
                throw new ArgumentException("Expression does not resolve to a property");
            }
        }

        public void BindChildren(
            Func<TModelNode, Task<ObservableCollection<TModelNode>>> getChildrenAsyncFunc)
        {
            this.getChildrenAsyncFunc = async n => await getChildrenAsyncFunc(n).ConfigureAwait(true);
        }

        public void BindChildren(
            Func<TModelNode, Task<ICollection<TModelNode>>> getChildrenAsyncFunc)
        {
            this.getChildrenAsyncFunc = getChildrenAsyncFunc;
        }

        //---------------------------------------------------------------------
        // Node
        //---------------------------------------------------------------------

        internal sealed class Node : TreeNode, IDisposable
        {
            private readonly BindableTreeView<TModelNode> treeView;
            public TModelNode Model { get; }

            //
            // Flag indicating whether a lazy load has been kicked off (it
            // might not be finished yet though).
            //
            private bool lazyLoadTriggered = false;

            private readonly TaskCompletionSource<ICollection<TModelNode>> lazyLoadResult
                = new TaskCompletionSource<ICollection<TModelNode>>();

            private readonly DisposableContainer bindings = new DisposableContainer();

            public Node(BindableTreeView<TModelNode> treeView, TModelNode modelNode)
            {
                this.treeView = treeView;
                this.Model = modelNode;

                //
                // Bind properties to keep TreeNode in sync with view model.
                // Note that binding is one-way (view model -> view) as TreeNodes
                // are not proper controls and do not provide the necessary events.
                //
                if (this.treeView.textExpression != null)
                {
                    this.Name = this.Text = this.treeView.textExpression.Compile()(this.Model);
                    this.bindings.Add(BindingExtensions.CreatePropertyChangeBinding(
                        this.Model,
                        this.treeView.textExpression,
                        text => this.Text = text));
                }

                if (this.treeView.imageIndexExpression != null)
                {
                    this.ImageIndex = this.treeView.imageIndexExpression.Compile()(this.Model);
                    if (!this.treeView.imageIndexExpressionReadonly)
                    {
                        this.bindings.Add(BindingExtensions.CreatePropertyChangeBinding(
                            this.Model,
                            this.treeView.imageIndexExpression,
                            iconIndex => this.ImageIndex = iconIndex));
                    }
                }

                if (this.treeView.selectedImageIndexExpression != null)
                {
                    this.SelectedImageIndex = this.treeView.selectedImageIndexExpression.Compile()(this.Model);
                    if (!this.treeView.selectedImageIndexExpressionReadonly)
                    {
                        this.bindings.Add(BindingExtensions.CreatePropertyChangeBinding(
                            this.Model,
                            this.treeView.selectedImageIndexExpression,
                            iconIndex => this.SelectedImageIndex = iconIndex));
                    }
                }

                if (this.treeView.isExpandedExpression != null)
                {
                    this.bindings.Add(BindingExtensions.CreatePropertyChangeBinding(
                        this.Model,
                        this.treeView.isExpandedExpression,
                        expanded =>
                        {
                            if (expanded)
                            {
                                Expand();
                            }
                            else
                            {
                                Collapse();
                            }
                        }));
                }

                if (this.treeView.isLeafFunc(this.Model))
                {
                    // This node does not have children.
                }
                else
                {
                    //
                    // This node might have children. Add a dummy node
                    // to ensure that the '+' control is being displayed.
                    //
                    this.Nodes.Add(new LoadingTreeNode());

                    if (this.treeView.isExpandedFunc(this.Model))
                    {
                        // Eagerly load children.
                        Expand();
                        LazyLoadChildren();
                    }
                    else
                    {
                        // Lazy load children. Nothing to do for now.
                    }
                }
            }

            internal void OnExpand()
            {
                if (!this.treeView.isExpandedFunc(this.Model))
                {
                    this.treeView.setExpandedFunc(this.Model, true);
                }

                LazyLoadChildren();
            }

            internal void OnCollapse()
            {
                if (this.treeView.isExpandedFunc(this.Model))
                {
                    this.treeView.setExpandedFunc(this.Model, false);
                }
            }

            private void LazyLoadChildren()
            {
                if (this.lazyLoadTriggered)
                {
                    return;
                }

                this.lazyLoadTriggered = true;
                this.treeView.getChildrenAsyncFunc(this.Model)
                    .ContinueWith(t =>
                    {
                        try
                        {
                            var children = t.Result;

                            try
                            {
                                //
                                // Suspend redrawing while updating nodes
                                // to reduce flicker and vertical scrolling.
                                //
                                this.treeView.BeginUpdate();

                                //
                                // Clear any dummy node if present.
                                //
                                Debug.Assert(!this.Nodes.OfType<Node>().Any());
                                DisposeAndClear(this.Nodes);

                                //
                                // Add nodes.
                                //
                                AddTreeNodesForModelNodes(children);
                            }
                            finally
                            {
                                this.treeView.EndUpdate();
                            }

                            //
                            // Observe for changes.
                            //
                            if (children is INotifyCollectionChanged observable)
                            {
                                observable.CollectionChanged += ModelChildrenChanged;
                            }

                            //
                            // Notify waiters (but only the first time, not on retry).
                            //
                            if (!this.lazyLoadResult.Task.IsCompleted)
                            {
                                this.lazyLoadResult.SetResult(children);
                            }
                        }
                        catch (Exception e)
                        {
                            //
                            // Reset state so that the action can be retried.
                            //
                            Collapse();
                            this.treeView.setExpandedFunc(this.Model, false);
                            this.lazyLoadTriggered = false;

                            //
                            // Report error.
                            //
                            this.treeView.LoadingChildrenFailed?.Invoke(
                                this.Model,
                                new ExceptionEventArgs(e));

                            //
                            // Notify waiters (but only the first time, not on retry).
                            //
                            if (!this.lazyLoadResult.Task.IsCompleted)
                            {
                                this.lazyLoadResult.SetException(e);
                            }
                        }
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.None,

                    // Continue on UI thread. 
                    // Note that there's a bug in the CLR that can cause
                    // TaskScheduler.FromCurrentSynchronizationContext() to become null.
                    // Therefore, use a task scheduler object captured previously.
                    // Cf. https://stackoverflow.com/questions/4659257/
                    this.treeView.taskScheduler);
            }

            public Task ExpandAsync()
            {
                Expand();

                //
                // Return task indicating the lazy load result. If the
                // node was expanded already/before, then this task might
                // have been completed for a while.
                //
                return this.lazyLoadResult.Task;
            }

            internal void Reload()
            {
                //
                // Clear existing children as they might no longer be valid.
                //
                Collapse();
                this.Nodes.Clear();

                //
                // Force-reload children.
                //
                this.lazyLoadTriggered = false;
                this.Nodes.Add(new LoadingTreeNode());
                LazyLoadChildren();
            }

            private void AddTreeNodesForModelNodes(IEnumerable<TModelNode> children)
            {
                foreach (var child in children)
                {
                    this.Nodes.Add(new Node(this.treeView, child));
                }
            }

            internal Node FindTreeNodeByModelNode(TModelNode modelNode)
            {
                return this.Nodes
                    .OfType<Node>()
                    .FirstOrDefault(n => n.Model.Equals(modelNode));
            }

            private void ModelChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        AddTreeNodesForModelNodes(e.NewItems.OfType<TModelNode>());
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        foreach (var oldModelItem in e.OldItems.OfType<TModelNode>())
                        {
                            var oldTreeNode = FindTreeNodeByModelNode(oldModelItem);
                            if (oldTreeNode != null)
                            {
                                this.Nodes.Remove(oldTreeNode);
                                oldTreeNode.Dispose();
                            }
                        }

                        break;

                    case NotifyCollectionChangedAction.Replace:
                        if (e.OldItems.Count == e.NewItems.Count)
                        {
                            var count = e.OldItems.Count;
                            for (var i = 0; i < count; i++)
                            {
                                var oldModelItem = (TModelNode)e.OldItems[i];
                                var newModelItem = (TModelNode)e.NewItems[i];

                                var treeNode = FindTreeNodeByModelNode(oldModelItem);
                                if (treeNode != null)
                                {
                                    this.Nodes.Remove(treeNode);
                                    treeNode.Dispose();

                                    this.Nodes.Insert(
                                        e.NewStartingIndex,
                                        new Node(this.treeView, newModelItem));
                                }
                            }
                        }

                        break;

                    case NotifyCollectionChangedAction.Move:
                        // Not supported.
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        // Reload everything.
                        DisposeAndClear(this.Nodes);

                        AddTreeNodesForModelNodes((ObservableCollection<TModelNode>)sender);
                        break;

                    default:
                        break;
                }
            }

            public void Dispose()
            {
                this.bindings.Dispose();
            }
        }

        private class LoadingTreeNode : TreeNode
        {
            public LoadingTreeNode()
                : base("Loading...")
            {
            }
        }
    }
}
