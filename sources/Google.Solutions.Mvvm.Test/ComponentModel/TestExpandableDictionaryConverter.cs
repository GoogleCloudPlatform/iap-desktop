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
    public class TestExpandableDictionaryConverter
    {
        [Test]
        public void GetProperties()
        {
            var dictionary = new Dictionary<string, object>()
            {
                { "key-a", "value-a" },
                { "key-b", 1 }
            };

            var converter = new ExpandableDictionaryConverter();
            var properties = converter.GetProperties(dictionary);

            Assert.AreEqual(2, properties.Count);

            Assert.AreEqual("key-a", properties[0].Name);
            Assert.AreEqual(typeof(string), properties[0].PropertyType);
            Assert.IsTrue(properties[0].IsBrowsable);
            Assert.IsTrue(properties[0].IsReadOnly);
            Assert.AreEqual("value-a", properties[0].GetValue(dictionary));

            Assert.AreEqual("key-b", properties[1].Name);
            Assert.AreEqual(typeof(int), properties[1].PropertyType);
            Assert.IsTrue(properties[1].IsBrowsable);
            Assert.IsTrue(properties[1].IsReadOnly);
            Assert.AreEqual(1, properties[1].GetValue(dictionary));
        }
    }
}
