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

using Google.Solutions.Mvvm.Binding;
using NUnit.Framework;
using System.Collections.Specialized;

namespace Google.Solutions.Mvvm.Test.Binding
{
    [TestFixture]
    public class TestRangeObservableCollection
    {
        [Test]
        public void Add_ThenIndividualEventsAreFired()
        {
            var callbacks = 0;
            var collection = new RangeObservableCollection<string>();
            collection.CollectionChanged += (sender, args) => { callbacks++; };

            Assert.That(callbacks, Is.EqualTo(0));
            collection.Add("one");
            collection.Add("two");
            Assert.That(callbacks, Is.EqualTo(2));
        }

        [Test]
        public void AddRange_ThenSingleEventIsFired()
        {
            var callbacks = 0;
            var collection = new RangeObservableCollection<string>();
            collection.CollectionChanged += (sender, args) =>
            {
                callbacks++;

                Assert.That(args.Action, Is.EqualTo(NotifyCollectionChangedAction.Reset));
            };

            Assert.That(callbacks, Is.EqualTo(0));
            collection.AddRange(new string[] { "one", "two", "three" });
            Assert.That(callbacks, Is.EqualTo(1));
        }
    }
}
