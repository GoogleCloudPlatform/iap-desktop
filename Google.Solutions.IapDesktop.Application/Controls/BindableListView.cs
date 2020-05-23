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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Controls
{
    /// <summary>
    /// Listview that support simple data binding.
    /// </summary>
    /// <typeparam name="TModelItem"></typeparam>
    public class BindableListView<TModelItem> : FlatListView
    {
        private ObservableCollection<TModelItem> model;
        private readonly IDictionary<int, Func<TModelItem, string>> columnAccessors =
            new Dictionary<int, Func<TModelItem, string>>();

        [Browsable(true)]
        public bool AutoResizeColumnsOnUpdate { get; set; } = false;

        private string ExtractColumnValue(int columnIndex, TModelItem modelItem)
        {
            if (this.columnAccessors.TryGetValue(columnIndex, out Func<TModelItem, string> accessorFunc))
            {
                return accessorFunc(modelItem);
            }
            else
            {
                return null;
            }
        }

        private ListViewItem FindViewItem(TModelItem modelItem)
        {
            return this.Items
                .OfType<ListViewItem>()
                .FirstOrDefault(item => Equals(item.Tag, modelItem));
        }

        private void AddViewItems(IEnumerable<TModelItem> items)
        {
            foreach (var item in items)
            {
                ObserveItem(item);
            }

            Items.AddRange(items
                .Select(item => new ListViewItem(
                    Columns.OfType<ColumnHeader>().Select(c => ExtractColumnValue(c.Index, item)).ToArray())
                {
                    Tag = item
                }).ToArray());
        }

        //---------------------------------------------------------------------
        // Binding.
        //---------------------------------------------------------------------

        public ObservableCollection<TModelItem> Model
        {
            get => this.model;
            set
            {
                // Reset.
                if (model != null)
                {
                    model.CollectionChanged -= Model_CollectionChanged;
                }

                this.Items.Clear();

                // Configure control.
                this.model = value;
                if (this.model != null)
                {
                    AddViewItems(this.model);

                    this.model.CollectionChanged += Model_CollectionChanged;
                }
            }
        }

        public void BindColumn(int columnIndex, Func<TModelItem, string> accessorFunc)
        {
            this.columnAccessors[columnIndex] = accessorFunc;
        }

        //---------------------------------------------------------------------
        // Change event handlers.
        //---------------------------------------------------------------------

        private void ObserveItem(TModelItem item)
        {
            if (item is INotifyPropertyChanged observableItem)
            {
                observableItem.PropertyChanged += ModelItem_PropertyChanged;
            }
        }

        private void UnobserveItem(TModelItem item)
        {
            if (item is INotifyPropertyChanged observableItem)
            {
                observableItem.PropertyChanged -= ModelItem_PropertyChanged;
            }
        }

        private void Model_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddViewItems(e.NewItems.OfType<TModelItem>());
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var oldModelItem in e.OldItems.OfType<TModelItem>())
                    {
                        var oldViewItem = FindViewItem(oldModelItem);
                        if (oldViewItem != null)
                        {
                            UnobserveItem(oldModelItem);
                            this.Items.Remove(oldViewItem);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (e.OldItems.Count == e.NewItems.Count)
                    {
                        var count = e.OldItems.Count;
                        for (var i = 0; i < count; i++)
                        {
                            var oldModelItem = (TModelItem)e.OldItems[i];
                            var newModelItem = (TModelItem)e.NewItems[i];

                            var viewItem = FindViewItem(oldModelItem);
                            if (viewItem != null)
                            {
                                UnobserveItem(oldModelItem);
                                ObserveItem(newModelItem);

                                viewItem.Tag = newModelItem;

                                foreach (ColumnHeader column in Columns)
                                {
                                    viewItem.SubItems[column.Index].Text =
                                        ExtractColumnValue(column.Index, newModelItem);
                                }
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    foreach (var oldItem in e.OldItems.OfType<TModelItem>())
                    {
                        var viewItem = FindViewItem(oldItem);
                        if (oldItem != null)
                        {
                            this.Items.Remove(viewItem);
                            this.Items.Insert(e.NewStartingIndex, viewItem);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    this.Items.Clear();
                    break;

                default:
                    break;
            }

            if (this.AutoResizeColumnsOnUpdate)
            {
                AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            }
        }

        private void ModelItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var modelItem = (TModelItem)sender;
            var viewItem = FindViewItem(modelItem);
            if (viewItem != null)
            {
                foreach (var column in this.Columns.OfType<ColumnHeader>())
                {
                    viewItem.SubItems[column.Index].Text = ExtractColumnValue(column.Index, modelItem);
                }
            }
        }
    }
}
