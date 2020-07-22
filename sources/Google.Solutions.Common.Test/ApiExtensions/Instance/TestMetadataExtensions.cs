//
// Copyright 2019 Google LLC
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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.Common.ApiExtensions.Instance;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.Common.Test.ApiExtensions.Instance
{
    [TestFixture]
    public class TestMetadataExtensions : FixtureBase
    {
        //---------------------------------------------------------------------
        // Add key/value
        //---------------------------------------------------------------------

        [Test]
        public void WhenMetadataIsEmpty_ThenAddKeyInsertsItem()
        {
            var metadata = new Metadata();
            metadata.Add("key", "value");

            Assert.AreEqual(1, metadata.Items.Count);
            Assert.AreEqual("value", metadata.Items.First(i => i.Key == "key").Value);
        }

        [Test]
        public void WhenMetadataContainsEntry_ThenAddKeyUpdatesItem()
        {
            var metadata = new Metadata()
            {
                Items = new List<Metadata.ItemsData>()
                {
                    new Metadata.ItemsData()
                    {
                        Key = "key",
                        Value = "existingvalue"
                    }
                }
            };
            metadata.Add("key", "value");

            Assert.AreEqual(1, metadata.Items.Count);
            Assert.AreEqual("value", metadata.Items.First(i => i.Key == "key").Value);
        }
        
        [Test]
        public void WhenMetadataContainsOtherEntry_ThenAddKeyInsertsItem()
        {
            var metadata = new Metadata()
            {
                Items = new List<Metadata.ItemsData>()
                {
                    new Metadata.ItemsData()
                    {
                        Key = "existingkey",
                        Value = "existingvalue"
                    }
                }
            };
            metadata.Add("key", "value");

            Assert.AreEqual(2, metadata.Items.Count);
            Assert.AreEqual("value", metadata.Items.First(i => i.Key == "key").Value);
            Assert.AreEqual("existingvalue", metadata.Items.First(i => i.Key == "existingkey").Value);
        }

        //---------------------------------------------------------------------
        // Add metadata
        //---------------------------------------------------------------------

        [Test]
        public void WhenMetadataIsEmpty_ThenAddMetadataInsertsItems()
        {
            var metadata = new Metadata();

            metadata.Add(new Metadata()
            {
                Items = new List<Metadata.ItemsData>()
                {
                    new Metadata.ItemsData()
                    {
                        Key = "key",
                        Value = "value"
                    }
                }
            });

            Assert.AreEqual(1, metadata.Items.Count);
            Assert.AreEqual("value", metadata.Items.First(i => i.Key == "key").Value);
        }

        [Test]
        public void WhenMetadataContainsOtherEntry_ThenAddMetadataInsertsItems()
        {
            var metadata = new Metadata()
            {
                Items = new List<Metadata.ItemsData>()
                {
                    new Metadata.ItemsData()
                    {
                        Key = "existingkey",
                        Value = "existingvalue"
                    }
                }
            };

            metadata.Add(new Metadata()
            {
                Items = new List<Metadata.ItemsData>()
                {
                    new Metadata.ItemsData()
                    {
                        Key = "key",
                        Value = "value"
                    }
                }
            });

            Assert.AreEqual(2, metadata.Items.Count);
            Assert.AreEqual("value", metadata.Items.First(i => i.Key == "key").Value);
            Assert.AreEqual("existingvalue", metadata.Items.First(i => i.Key == "existingkey").Value);
        }

        [Test]
        public void WhenMetadataContainsEntry_ThenAddMetadataUpdatesItems()
        {
            var metadata = new Metadata()
            {
                Items = new List<Metadata.ItemsData>()
                {
                    new Metadata.ItemsData()
                    {
                        Key = "key",
                        Value = "existingvalue"
                    }
                }
            };

            metadata.Add(new Metadata()
            {
                Items = new List<Metadata.ItemsData>()
                {
                    new Metadata.ItemsData()
                    {
                        Key = "key",
                        Value = "newvalue"
                    }
                }
            });

            Assert.AreEqual(1, metadata.Items.Count);
            Assert.AreEqual("newvalue", metadata.Items.First(i => i.Key == "key").Value);
        }


        //---------------------------------------------------------------------
        // AsString
        //---------------------------------------------------------------------

        [Test]
        public void WhenMetadataIsEmpty_ThenAsStringReturnsEmptyList()
        {
            var metadata = new Metadata();

            Assert.AreEqual("[]", metadata.AsString());
        }

        [Test]
        public void WhenMetadataContainsEntries_ThenAsStringReturnsList()
        {
            var metadata = new Metadata()
            {
                Items = new List<Metadata.ItemsData>()
                {
                    new Metadata.ItemsData()
                    {
                        Key = "foo",
                        Value = "foovalue"
                    },
                    new Metadata.ItemsData()
                    {
                        Key = "bar",
                        Value = "barvalue"
                    }
                }
            };

            Assert.AreEqual("[foo=foovalue, bar=barvalue]", metadata.AsString());
        }
    }
}
