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
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    /// <summary>
    /// Listview that support simple data binding.
    /// </summary>
    public class BindableListView<TModelItem> : ListView
    {
        private ICollection<TModelItem>? model;

        private readonly IDictionary<int, Func<TModelItem, string>> columnAccessors =
            new Dictionary<int, Func<TModelItem, string>>();
        private Func<TModelItem, int>? imageIndexAccessor = null;

        [Browsable(true)]
        public bool AutoResizeColumnsOnUpdate { get; set; } = false;

        private string? ExtractColumnValue(int columnIndex, TModelItem modelItem)
        {
            if (this.columnAccessors.TryGetValue(columnIndex, out var accessorFunc))
            {
                return accessorFunc(modelItem);
            }
            else
            {
                return null;
            }
        }

        private int ExtractImageIndex(TModelItem modelItem)
        {
            return this.imageIndexAccessor == null
                ? 0 :
                this.imageIndexAccessor(modelItem);
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

            this.Items.AddRange(items
                .Select(item => new ListViewItem(
                    this.Columns
                        .OfType<ColumnHeader>()
                        .Select(c => ExtractColumnValue(c.Index, item))
                        .ToArray())
                {
                    Tag = item,
                    ImageIndex = ExtractImageIndex(item)
                }).ToArray());
        }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public BindableListView()
        {
            //
            // By default, double buffering is off. Especially in dark mode,
            // this causes visible flickering.
            //
            this.DoubleBuffered = true;
        }


        //---------------------------------------------------------------------
        // Selection properties.
        //---------------------------------------------------------------------

        public event EventHandler? SelectedModelItemsChanged;
        public event EventHandler? SelectedModelItemChanged;

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            this.SelectedModelItemsChanged?.Invoke(this, e);
            this.SelectedModelItemChanged?.Invoke(this, e);
        }

        public IEnumerable<TModelItem> SelectedModelItems
        {
            get => this.SelectedItems
                .OfType<ListViewItem>()
                .Select(item => (TModelItem)item.Tag);
            set
            {
                this.SelectedIndices.Clear();

                if (value != null)
                {
                    foreach (var selectedItem in value)
                    {
                        var index = this.Items.IndexOf(FindViewItem(selectedItem));
                        Debug.Assert(index >= 0);
                        this.SelectedIndices.Add(index);
                    }
                }

                this.SelectedModelItemsChanged?.Invoke(this, EventArgs.Empty);
            }
        }


        public TModelItem SelectedModelItem
        {
            get => this.SelectedItems
                .OfType<ListViewItem>()
                .Select(item => (TModelItem)item.Tag)
                .FirstOrDefault();
            set
            {
                this.SelectedIndices.Clear();

                if (value != null)
                {
                    var index = this.Items.IndexOf(FindViewItem(value));
                    Debug.Assert(index >= 0);
                    this.SelectedIndices.Add(index);
                }

                this.SelectedModelItemChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        //---------------------------------------------------------------------
        // List Binding.
        //---------------------------------------------------------------------

        public void BindCollection(ICollection<TModelItem> model)
        {
            // Reset.
            if (this.model != null)
            {
                UnobserveItems(this.model);

                if (this.model is INotifyCollectionChanged observable)
                {
                    observable.CollectionChanged -= Model_CollectionChanged;
                }
            }

            this.Items.Clear();

            // Configure control.
            this.model = model;
            if (this.model != null)
            {
                AddViewItems(this.model);

                if (this.model is INotifyCollectionChanged observable)
                {
                    observable.CollectionChanged += Model_CollectionChanged;
                }
            }
        }

        public void BindColumn(int columnIndex, Func<TModelItem, string> accessorFunc)
        {
            this.columnAccessors[columnIndex] = accessorFunc;
        }

        public void BindImageIndex(Func<TModelItem, int> accessorFunc)
        {
            this.imageIndexAccessor = accessorFunc;
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

        private void UnobserveItems(IEnumerable<TModelItem> items)
        {
            foreach (var item in items)
            {
                UnobserveItem(item);
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

                                foreach (ColumnHeader column in this.Columns)
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
                    // Reload everything.
                    UnobserveItems(this.Items.Cast<ListViewItem>()
                        .Select(i => i.Tag)
                        .OfType<TModelItem>());

                    this.Items.Clear();
                    AddViewItems(this.model.EnsureNotNull());

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
            // Rehydrate the entire list item.

            var modelItem = (TModelItem)sender;
            var viewItem = FindViewItem(modelItem);
            if (viewItem != null)
            {
                viewItem.ImageIndex = ExtractImageIndex(modelItem);

                foreach (var column in this.Columns.OfType<ColumnHeader>())
                {
                    viewItem.SubItems[column.Index].Text = ExtractColumnValue(column.Index, modelItem);
                }
            }
        }
    }
}
