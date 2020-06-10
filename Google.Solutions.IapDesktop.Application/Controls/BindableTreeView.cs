using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

#pragma warning disable CS1690 // Accessing a member on a field of a marshal-by-reference class may cause a runtime exception

namespace Google.Solutions.IapDesktop.Application.Controls
{
    public class BindableTreeView<TModelNode> : TreeView
        where TModelNode : class, INotifyPropertyChanged
    {
        private Expression<Func<TModelNode, bool>> IsExpandedExpression = null;
        private Expression<Func<TModelNode, int>> ImageIndexExpression = null;
        private Expression<Func<TModelNode, int>> SelectedImageIndexExpression = null;
        private Expression<Func<TModelNode, string>> TextExpression = null;
        
        private Func<TModelNode, bool> IsLeaf = _ => false;
        private Action<TModelNode, bool> SetExpanded = (n, state) => {};
        private Func<TModelNode, bool> IsExpanded = _ => false;
        private Func<TModelNode, Task<ObservableCollection<TModelNode>>> GetChildrenAsync
            = _ => Task.FromResult(new ObservableCollection<TModelNode>());

        public event EventHandler SelectedModelNodeChanged;

        public BindableTreeView()
        {
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

        public TModelNode SelectedModelNode
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

        public void Bind(TModelNode rootNode)
        {
            this.Nodes.Clear();
            this.Nodes.Add(new Node(this, rootNode));
        }

        public void BindImageIndex(Expression<Func<TModelNode, int>> imageIndexExpression)
        {
            this.ImageIndexExpression = imageIndexExpression;
        }

        public void BindIsLeaf(Func<TModelNode, bool> isLeafFunc)
        {
            this.IsLeaf = isLeafFunc;
        }

        public void BindSelectedImageIndex(Expression<Func<TModelNode, int>> selectedImageIndexExpression)
        {
            this.SelectedImageIndexExpression = selectedImageIndexExpression;
        }

        public void BindText(Expression<Func<TModelNode, string>> nameExpression)
        {
            this.TextExpression = nameExpression;
        }

        public void BindIsExpanded(Expression<Func<TModelNode, bool>> propertyExpression)
        {
            if (propertyExpression.Body is MemberExpression memberExpression &&
                memberExpression.Member is PropertyInfo propertyInfo)
            {
                this.IsExpandedExpression = propertyExpression;
                this.IsExpanded = propertyExpression.Compile();
                this.SetExpanded = (node, value) => propertyInfo.SetValue(node, value);
            }
            else
            {
                throw new ArgumentException("Expression does not resolve to a property");
            }
        }

        public void BindChildren(
            Func<TModelNode, Task<ObservableCollection<TModelNode>>> getChildrenAsyncFunc)
        {
            this.GetChildrenAsync = getChildrenAsyncFunc;
        }

        //---------------------------------------------------------------------
        // BindableTreeNode
        //---------------------------------------------------------------------

        private class Node : TreeNode 
        {
            private readonly BindableTreeView<TModelNode> treeView;
            public TModelNode Model { get; }
            private bool lazyLoadTriggered = false;

            public Node(BindableTreeView<TModelNode> treeView, TModelNode modelNode)
            {
                this.treeView = treeView;
                this.Model = modelNode;


                // Bind properties to keep TreeNode in sync with view model.
                // Note that binding is one-way (view model -> view) as TreeNodes
                // are not proper controls and do not provide the necessary events.
                if (this.treeView.TextExpression != null)
                {
                    this.Name = this.Text = this.treeView.TextExpression.Compile()(this.Model);
                    this.Model.OnPropertyChange(
                        this.treeView.TextExpression,
                        text => this.Text = text);
                }

                if (this.treeView.ImageIndexExpression != null)
                {
                    this.ImageIndex = this.treeView.ImageIndexExpression.Compile()(this.Model);
                    this.Model.OnPropertyChange(
                        this.treeView.ImageIndexExpression,
                        iconIndex => this.ImageIndex = iconIndex);
                }

                if (this.treeView.SelectedImageIndexExpression != null)
                {
                    this.SelectedImageIndex = this.treeView.SelectedImageIndexExpression.Compile()(this.Model);
                    this.Model.OnPropertyChange(
                        this.treeView.SelectedImageIndexExpression,
                        iconIndex => this.SelectedImageIndex = iconIndex);
                }

                if (this.treeView.IsExpandedExpression != null)
                {
                    this.Model.OnPropertyChange(
                        this.treeView.IsExpandedExpression,
                        expanded =>
                        {
                            if (expanded)
                            {
                                this.Expand();
                            }
                            else
                            {
                                this.Collapse();
                            }
                        });
                }

                if (this.treeView.IsLeaf(this.Model))
                {
                    // This node does not have children.
                }
                else
                {
                    // This node might have children. Add a dummy node
                    // to ensure that the '+' control is being displayed.
                    this.Nodes.Add(new LoadingTreeNode());

                    if (this.treeView.IsExpanded(this.Model))
                    {
                        // Eagerly load children.
                        this.Expand();
                        LazyLoadChildren();
                    }
                    else
                    {
                        // Lazy load children. Nothing to do for now.
                    }
                }
            }

            //private bool IsLoaded
            //{
            //    get
            //    {
            //        if (this.treeView.IsLeaf(this.Model))
            //        {
            //            // This node does not have children.
            //            return true;
            //        }
            //        else
            //        {
            //            return !(this.Nodes.OfType<LoadingTreeNode>().Any());
            //        }
            //    }
            //}

            internal void OnExpand()
            {
                if (!this.treeView.IsExpanded(this.Model))
                {
                    this.treeView.SetExpanded(this.Model, true);
                }

                LazyLoadChildren();
            }

            internal void OnCollapse()
            {
                if (this.treeView.IsExpanded(this.Model))
                {
                    this.treeView.SetExpanded(this.Model, false);
                }
            }

            public void LazyLoadChildren()
            {
                if (this.lazyLoadTriggered)
                {
                    return;
                }

                this.lazyLoadTriggered = true;
                this.treeView.GetChildrenAsync(this.Model)
                    .ContinueWith(t =>
                    {
                        var children = t.Result;

                        // Clear any dummy node if present.
                        this.Nodes.Clear();

                        // Add nodes.
                        AddTreeNodesForModelNodes(children);

                        // Observe for changes.
                        children.CollectionChanged += ModelChildrenChanged;
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.None,
                    TaskScheduler.FromCurrentSynchronizationContext()); // Continue on UI thread.
            }

            private void AddTreeNodesForModelNodes(IEnumerable<TModelNode> children)
            {
                foreach (var child in children)
                {
                    this.Nodes.Add(new Node(this.treeView, child));
                }
            }

            private Node FindTreeNodeByModelNode(TModelNode modelNode)
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
                        this.Nodes.Clear();
                        AddTreeNodesForModelNodes((ObservableCollection<TModelNode>)sender);
                        break;

                    default:
                        break;
                }
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
