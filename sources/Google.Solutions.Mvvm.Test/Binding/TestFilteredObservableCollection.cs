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

using Google.Solutions.Mvvm.Binding;
using NUnit.Framework;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Google.Solutions.Mvvm.Test.Binding
{
    [TestFixture]
    public class TestFilteredObservableCollection
    {
        [Test]
        public void IsReadOnly()
        {
            var collection = new FilteredObservableCollection<string>(
                new ObservableCollection<string>());

            Assert.IsTrue(collection.IsReadOnly);
        }

        [Test]
        public void Add()
        {
            var collection = new FilteredObservableCollection<string>(
                new ObservableCollection<string>());

            Assert.Throws<InvalidOperationException>(() => collection.Add(""));
        }

        [Test]
        public void Clear()
        {
            var collection = new FilteredObservableCollection<string>(
                new ObservableCollection<string>());

            Assert.Throws<InvalidOperationException>(() => collection.Clear());
        }

        [Test]
        public void Remove()
        {
            var collection = new FilteredObservableCollection<string>(
                new ObservableCollection<string>());

            Assert.Throws<InvalidOperationException>(() => collection.Remove(""));
        }

        [Test]
        public void Contains()
        {
            var collection = new ObservableCollection<string>
            {
                "one",
                "Two",
                "THREE"
            };

            var filtered = new FilteredObservableCollection<string>(collection)
            {
                Predicate = s => s == s.ToUpper()
            };

            Assert.That(filtered.Contains("one"), Is.False);
            Assert.That(filtered.Contains("Two"), Is.False);
            Assert.IsTrue(filtered.Contains("THREE"));
        }

        [Test]
        public void Count()
        {
            var collection = new ObservableCollection<string>
            {
                "one",
                "Two",
                "THREE"
            };

            var filtered = new FilteredObservableCollection<string>(collection)
            {
                Predicate = s => s == s.ToUpper()
            };

            Assert.That(filtered.Count, Is.EqualTo(1));
        }

        [Test]
        public void CopyTo()
        {
            var collection = new ObservableCollection<string>
            {
                "one",
                "Two",
                "THREE"
            };

            var filtered = new FilteredObservableCollection<string>(collection)
            {
                Predicate = s => s == s.ToUpper()
            };

            var result = new string[2];
            filtered.CopyTo(result, 1);
            Assert.IsNull(result[0]);
            Assert.That(result[1], Is.EqualTo("THREE"));
        }

        [Test]
        public void GetEnumerator()
        {
            var collection = new ObservableCollection<string>
            {
                "one",
                "Two",
                "THREE"
            };

            var filtered = new FilteredObservableCollection<string>(collection)
            {
                Predicate = s => s == s.ToUpper()
            };

            Assert.That(
                filtered.ToList(), Is.EquivalentTo(new[] { "THREE" }));
        }

        [Test]
        public void Predicate_WhenPredicateChanged_ThenResetEventIsRaised()
        {
            var collection = new ObservableCollection<string>
            {
                "one",
                "Two",
                "THREE"
            };

            var filtered = new FilteredObservableCollection<string>(collection)
            {
                Predicate = s => s == s.ToUpper()
            };

            var eventRaised = false;
            filtered.CollectionChanged += (_, args) =>
            {
                eventRaised = true;
                Assert.That(args.Action, Is.EqualTo(NotifyCollectionChangedAction.Reset));
            };

            filtered.Predicate = s => s == s.ToLower();

            Assert.IsTrue(eventRaised);
        }

        [Test]
        public void Add_WhenAddingMatchedItem_ThenNoAddEventIsRaised()
        {
            var collection = new ObservableCollection<string>();

            var filtered = new FilteredObservableCollection<string>(collection)
            {
                Predicate = s => s == s.ToUpper()
            };

            var eventRaised = false;
            filtered.CollectionChanged += (_, args) =>
            {
                eventRaised = true;
                Assert.That(args.Action, Is.EqualTo(NotifyCollectionChangedAction.Add));
                Assert.That(
                    args.NewItems, Is.EquivalentTo(new[] { "UPPERCASE" }));
            };

            collection.Add("UPPERCASE");
            collection.Add("lowercase");

            Assert.IsTrue(eventRaised);
        }

        [Test]
        public void Add_WhenAddingUnmatchedItem_ThenNoAddEventIsRaised()
        {
            var collection = new ObservableCollection<string>();

            var filtered = new FilteredObservableCollection<string>(collection)
            {
                Predicate = s => s == s.ToUpper()
            };

            var eventRaised = false;
            filtered.CollectionChanged += (_, args) =>
            {
                eventRaised = true;
            };

            collection.Add("lowercase");

            Assert.That(eventRaised, Is.False);
        }


        [Test]
        public void Remove_WhenRemovingMatchedItem_ThenNoAddEventIsRaised()
        {
            var collection = new ObservableCollection<string>
            {
                "UPPERCASE",
                "lowercase"
            };

            var filtered = new FilteredObservableCollection<string>(collection)
            {
                Predicate = s => s == s.ToUpper()
            };

            var eventRaised = false;
            filtered.CollectionChanged += (_, args) =>
            {
                eventRaised = true;
                Assert.That(args.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
                Assert.That(
                    args.OldItems, Is.EquivalentTo(new[] { "UPPERCASE" }));
            };

            collection.Remove("UPPERCASE");
            collection.Remove("lowercase");

            Assert.IsTrue(eventRaised);
        }

        [Test]
        public void Remove_WhenRemovingUnmatchedItem_ThenNoAddEventIsRaised()
        {
            var collection = new ObservableCollection<string>
            {
                "lowercase"
            };

            var filtered = new FilteredObservableCollection<string>(collection)
            {
                Predicate = s => s == s.ToUpper()
            };

            var eventRaised = false;
            filtered.CollectionChanged += (_, args) =>
            {
                eventRaised = true;
            };

            collection.Remove("lowercase");

            Assert.That(eventRaised, Is.False);
        }

        [Test]
        public void Clear_WhenClearingCollection_ThenResetIsRaised()
        {
            var collection = new ObservableCollection<string>
            {
                "lowercase"
            };

            var filtered = new FilteredObservableCollection<string>(collection)
            {
                Predicate = s => s == s.ToUpper()
            };

            var eventRaised = false;
            filtered.CollectionChanged += (_, args) =>
            {
                eventRaised = true;
                Assert.That(args.Action, Is.EqualTo(NotifyCollectionChangedAction.Reset));
            };

            collection.Clear();

            Assert.IsTrue(eventRaised);
        }
    }
}
