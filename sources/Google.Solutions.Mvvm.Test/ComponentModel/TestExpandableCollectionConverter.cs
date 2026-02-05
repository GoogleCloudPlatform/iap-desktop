//
// Copyright 2024 Google LLC
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

using Google.Solutions.Mvvm.ComponentModel;
using NUnit.Framework;
using System.Collections.Generic;

namespace Google.Solutions.Mvvm.Test.ComponentModel
{
    [TestFixture]
    public class TestExpandableCollectionConverter
    {
        [Test]
        public void Dictionary()
        {
            var dictionary = new Dictionary<string, object>()
            {
                { "key-a", "value-a" },
                { "key-b", 1 }
            };

            var converter = new ExpandableCollectionConverter();
            var properties = converter.GetProperties(dictionary);

            Assert.That(properties.Count, Is.EqualTo(2));

            Assert.That(properties[0].Name, Is.EqualTo("key-a"));
            Assert.That(properties[0].PropertyType, Is.EqualTo(typeof(string)));
            Assert.That(properties[0].IsBrowsable, Is.True);
            Assert.That(properties[0].IsReadOnly, Is.True);
            Assert.That(properties[0].GetValue(dictionary), Is.EqualTo("value-a"));

            Assert.That(properties[1].Name, Is.EqualTo("key-b"));
            Assert.That(properties[1].PropertyType, Is.EqualTo(typeof(int)));
            Assert.That(properties[1].IsBrowsable, Is.True);
            Assert.That(properties[1].IsReadOnly, Is.True);
            Assert.That(properties[1].GetValue(dictionary), Is.EqualTo(1));
        }

        [Test]
        public void Array()
        {
            var array = new[] { "value-a", "value-b" };

            var converter = new ExpandableCollectionConverter();
            var properties = converter.GetProperties(array);

            Assert.That(properties.Count, Is.EqualTo(2));

            Assert.That(properties[0].Name, Is.EqualTo(" "));
            Assert.That(properties[0].PropertyType, Is.EqualTo(typeof(string)));
            Assert.That(properties[0].IsBrowsable, Is.True);
            Assert.That(properties[0].IsReadOnly, Is.True);
            Assert.That(properties[0].GetValue(array), Is.EqualTo("value-a"));

            Assert.That(properties[1].Name, Is.EqualTo(" "));
            Assert.That(properties[1].PropertyType, Is.EqualTo(typeof(string)));
            Assert.That(properties[1].IsBrowsable, Is.True);
            Assert.That(properties[1].IsReadOnly, Is.True);
            Assert.That(properties[1].GetValue(array), Is.EqualTo("value-b"));
        }
    }
}
