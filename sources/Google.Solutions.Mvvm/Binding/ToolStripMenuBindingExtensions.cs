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

using Google.Solutions.Common.Runtime;
using Google.Solutions.Common.Util;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Binding
{
    public static class ToolStripMenuBindingExtensions
    {
        public static void BindItem<TModel>(
            this ToolStripItem item,
            TModel model,
            Func<TModel, bool> isSeparator,
            Expression<Func<TModel, string?>> getText,
            Expression<Func<TModel, string?>> getToolTip,
            Expression<Func<TModel, Image?>> getImage,
            Expression<Func<TModel, Keys>> getShortcuts,
            Expression<Func<TModel, bool>> isVisible,
            Expression<Func<TModel, bool>> isEnabled,
            Expression<Func<TModel, ToolStripItemDisplayStyle>> getStyle,
            Func<TModel, ObservableCollection<TModel>> getChildren,
            Action<TModel> click,
            IBindingContext bindingContext)
            where TModel : class, INotifyPropertyChanged
        {
            item.BindReadonlyProperty(
                c => c.Text,
                model,
                getText,
                bindingContext);
            item.BindReadonlyProperty(
                c => c.ToolTipText,
                model,
                getToolTip,
                bindingContext);
            item.BindReadonlyProperty(
                c => c.Image,
                model,
                getImage,
                bindingContext);
            item.BindReadonlyProperty(
                c => c.Visible,
                model,
                isVisible,
                bindingContext);
            item.BindReadonlyProperty(
                c => c.Enabled,
                model,
                isEnabled,
                bindingContext);
            item.BindReadonlyProperty(
                c => c.DisplayStyle,
                model,
                getStyle,
                bindingContext);

            void OnClick(object sender, EventArgs args)
                => click(model);

            item.Click += OnClick;
            bindingContext.OnBindingCreated(
                item,
                Disposable.For(() => item.Click -= OnClick));

            if (item is ToolStripMenuItem toolStripMenuItem)
            {
                toolStripMenuItem.BindReadonlyProperty(
                    c => c.ShortcutKeys,
                    model,
                    getShortcuts,
                    bindingContext);

                var subCommands = getChildren(model);
                if (subCommands != null)
                {
                    toolStripMenuItem.DropDownItems.BindCollection(
                        subCommands,
                        isSeparator,
                        getText,
                        getToolTip,
                        getImage,
                        getShortcuts,
                        isVisible,
                        isEnabled,
                        getStyle,
                        getChildren,
                        click,
                        bindingContext);
                }
            }
        }

        public static void BindCollection<TModel>(
            this ToolStripItemCollection view,
            ObservableCollection<TModel> modelCollection,
            Func<TModel, bool> isSeparator,
            Expression<Func<TModel, string?>> getText,
            Expression<Func<TModel, string?>> getToolTip,
            Expression<Func<TModel, Image?>> getImage,
            Expression<Func<TModel, Keys>> getShortcuts,
            Expression<Func<TModel, bool>> isVisible,
            Expression<Func<TModel, bool>> isEnabled,
            Expression<Func<TModel, ToolStripItemDisplayStyle>> getStyle,
            Func<TModel, ObservableCollection<TModel>> getChildren,
            Action<TModel> click,
            IBindingContext bindingContext)
            where TModel : class, INotifyPropertyChanged
        {
            ToolStripItem CreateMenuItem(TModel model)
            {
                if (isSeparator(model))
                {
                    return new ToolStripSeparator()
                    {
                        Tag = model
                    };
                }
                else
                {
                    //
                    // NB. If the display style is Image, then this must be 
                    // a toolbar. For toolbars, using ToolStripButton ensures
                    // that the button and its hottracking rectangle is sized
                    // correctly.
                    //
                    var item =
                        getStyle.Compile()(model) == ToolStripItemDisplayStyle.Image
                            ? (ToolStripItem)new ToolStripButton()
                            : (ToolStripItem)new ToolStripMenuItem();

                    item.Tag = model;

                    item.BindItem(
                        model,
                        isSeparator,
                        getText,
                        getToolTip,
                        getImage,
                        getShortcuts,
                        isVisible,
                        isEnabled,
                        getStyle,
                        getChildren,
                        click,
                        bindingContext);

                    return item;
                }
            }

            //
            // Do initial population.
            //
            view.AddRange(modelCollection
                .Select(modelItem => CreateMenuItem(modelItem))
                .ToArray());

            //
            // Propagate changes.
            //
            var binding = new Binding<TModel>(
                view,
                modelCollection,
                CreateMenuItem);

            //
            // NB. ToolStripItemCollection aren't componnets, so we
            // cannot report this binding to the binding context.
            //
        }

        private sealed class Binding<TModel> : IDisposable
        {
            private readonly ToolStripItemCollection view;
            private readonly ObservableCollection<TModel> model;
            private readonly Func<TModel, ToolStripItem> createMenuItem;

            public Binding(
                ToolStripItemCollection view,
                ObservableCollection<TModel> model,
                Func<TModel, ToolStripItem> createMenuItem)
            {
                this.view = view;
                this.model = model;
                this.createMenuItem = createMenuItem;

                this.model.CollectionChanged += Model_CollectionChanged;
            }

            private void Model_CollectionChanged(
                object sender,
                NotifyCollectionChangedEventArgs e)
            {
                var newModelItems = e.NewItems?.OfType<TModel>();
                var oldModelItems = e.OldItems?.OfType<TModel>();

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        {
                            var index = e.NewStartingIndex;
                            foreach (var newModelItem in newModelItems.EnsureNotNull())
                            {
                                this.view.Insert(
                                    index++,
                                    this.createMenuItem(newModelItem));
                            }
                        }
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        {
                            foreach (var oldViewItem in this.view
                                .OfType<ToolStripMenuItem>()
                                .Where(item => item.Tag is TModel)
                                .Where(item => oldModelItems.Contains((TModel)item.Tag))
                                .ToList())
                            {
                                this.view.Remove(oldViewItem);
                            }
                        }
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        {
                            this.view.Clear();
                        }
                        break;

                    case NotifyCollectionChangedAction.Replace:
                    case NotifyCollectionChangedAction.Move:
                        throw new NotImplementedException();
                }
            }

            public void Dispose()
            {
                this.model.CollectionChanged -= Model_CollectionChanged;
            }
        }
    }
}
