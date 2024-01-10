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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Google.Solutions.Mvvm.Binding
{
    public class FilteredObservableCollection<T>
        : ICollection<T>, IEnumerable<T>, ICollection, IEnumerable, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private readonly ObservableCollection<T> collection;
        private Predicate<T> predicate = _ => true;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public FilteredObservableCollection(ObservableCollection<T> collection)
        {
            this.collection = collection;
            this.collection.CollectionChanged += (_, args) =>
            {
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        {
                            var filteredArgs = new NotifyCollectionChangedEventArgs(
                                args.Action,
                                args.NewItems.Cast<T>().Where(i => this.predicate(i)).ToList());

                            if (filteredArgs.NewItems.Count > 0)
                            {
                                this.CollectionChanged?.Invoke(this, filteredArgs);
                            }
                        }
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        {
                            var filteredArgs = new NotifyCollectionChangedEventArgs(
                                args.Action,
                                args.OldItems.Cast<T>().Where(i => this.predicate(i)).ToList());

                            if (filteredArgs.OldItems.Count > 0)
                            {
                                this.CollectionChanged?.Invoke(this, filteredArgs);
                            }
                        }
                        break;

                    case NotifyCollectionChangedAction.Replace:
                        {
                            this.CollectionChanged?.Invoke(
                                this,
                                new NotifyCollectionChangedEventArgs(
                                    args.Action,
                                    args.NewItems.Cast<T>().Where(i => this.predicate(i)).ToList(),
                                    args.OldItems.Cast<T>().Where(i => this.predicate(i)).ToList()));
                        }
                        break;

                    case NotifyCollectionChangedAction.Move:
                        //
                        // Ignore.
                        //
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        this.CollectionChanged?.Invoke(
                            this,
                            new NotifyCollectionChangedEventArgs(args.Action));
                        break;
                }
            };

            ((INotifyPropertyChanged)this.collection).PropertyChanged += (_, args) =>
            {
                this.PropertyChanged?.Invoke(this, args);
            };
        }

        public Predicate<T> Predicate
        {
            get => this.predicate;
            set
            {
                this.predicate = value;

                this.CollectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public bool IsFixedSize
            => ((IList)this.collection).IsFixedSize;

        public bool IsReadOnly
            => true;

        public bool IsSynchronized
            => ((IList)this.collection).IsSynchronized;

        public object SyncRoot
            => ((IList)this.collection).SyncRoot;

        //---------------------------------------------------------------------
        // Modification.
        //---------------------------------------------------------------------

        public void Add(T item)
            => throw new InvalidOperationException("List is immutable");

        public void Clear()
            => throw new InvalidOperationException("List is immutable");

        public bool Remove(T item)
            => throw new InvalidOperationException("List is immutable");

        //---------------------------------------------------------------------
        // Lookup.
        //---------------------------------------------------------------------

        public bool Contains(T item)
            => this.collection.Contains(item) && this.predicate(item);

        public bool Contains(object value)
            => Contains((T)value);

        //---------------------------------------------------------------------
        // Enumeration.
        //---------------------------------------------------------------------

        public void CopyTo(T[] array, int index)
            => this.collection
                .Where(i => this.predicate(i))
                .ToList()
                .CopyTo(array, index);

        public void CopyTo(Array array, int index)
            => CopyTo((T[])array, index);

        public int Count
            => this.collection.Where(i => this.predicate(i)).Count();

        public IEnumerator<T> GetEnumerator()
            => this.collection.Where(i => this.predicate(i)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => this.collection.Where(i => this.predicate(i)).GetEnumerator();
    }
}