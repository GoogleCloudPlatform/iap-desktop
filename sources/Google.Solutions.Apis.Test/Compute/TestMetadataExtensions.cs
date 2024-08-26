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
using Google.Solutions.Apis.Compute;
using Google.Solutions.Common.Test;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.Apis.Test.Compute
{
    [TestFixture]
    public class TestMetadataExtensions : CommonFixtureBase
    {
        //---------------------------------------------------------------------
        // Add key/value
        //---------------------------------------------------------------------

        [Test]
        public void Add_WhenMetadataIsEmpty_ThenInsertsItem()
        {
            var metadata = new Metadata();
            metadata.Add("key", "value");

            Assert.AreEqual(1, metadata.Items.Count);
            Assert.AreEqual("value", metadata.Items.First(i => i.Key == "key").Value);
        }

        [Test]
        public void Add_WhenMetadataContainsEntry_ThenUpdatesItem()
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
        public void Add_WhenMetadataContainsOtherEntry_ThenInsertsItem()
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
        public void AddMetadata_WhenMetadataIsEmpty_ThenInsertsItems()
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
        public void AddMetadata_WhenMetadataContainsOtherEntry_ThenInsertsItems()
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
        public void AddMetadata_WhenMetadataContainsEntry_ThenUpdatesItems()
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
        // GetValue.
        //---------------------------------------------------------------------

        [Test]
        public void GetValue_WhenMetadataIsNull_ThenReturnsNull()
        {
            var metadata = new Metadata();
            Assert.IsNull(metadata.GetValue("key"));
        }

        [Test]
        public void GetValue_WhenItemNotPresent_ThenReturnsNull()
        {
            var metadata = new Metadata()
            {
                Items = new[]
                {
                    new Metadata.ItemsData()
                    {
                        Key = "key"
                    }
                }
            };

            Assert.IsNull(metadata.GetValue("key"));
        }

        [Test]
        public void GetValue_WhenItemPresent_ThenReturnsValue()
        {

            var metadata = new Metadata()
            {
                Items = new[]
                {
                    new Metadata.ItemsData()
                    {
                        Key = "key",
                        Value = "value"
                    }
                }
            };

            Assert.AreEqual("value", metadata.GetValue("key"));
        }

        //---------------------------------------------------------------------
        // GetValue (Metadata).
        //---------------------------------------------------------------------

        [Test]
        public void GetFlag_WhenMetadataIsNull_ThenReturnsNull()
        {
            Assert.IsNull(MetadataExtensions.GetFlag((Metadata?)null, "flag"));
        }

        [Test]
        public void GetFlag_WhenMetadataItemsIsNull_ThenReturnsNull()
        {
            Assert.IsNull(new Metadata().GetFlag("flag"));
        }

        [Test]
        public void GetFlag_WhenValueNull_ThenReturnsNull()
        {
            var metadata = new Metadata()
            {
                Items = new[]
                {
                    new Metadata.ItemsData()
                    {
                        Key = "flag",
                        Value = null
                    }
                }
            };

            Assert.IsNull(metadata.GetFlag("flag"));
        }

        [Test]
        public void GetFlag_WhenValueIsTruthy_ThenReturnsTrue(
            [Values("Y", "y\n", "True ", " 1 ")] string truthyValue)
        {
            var metadata = new Metadata()
            {
                Items = new[]
                {
                    new Metadata.ItemsData()
                    {
                        Key = "flag",
                        Value = truthyValue
                    }
                }
            };

            Assert.IsTrue(metadata.GetFlag("flag"));
        }

        [Test]
        public void GetFlag_WhenValueIsNotTruthy_ThenReturnsFalse(
            [Values("N", " no\n", "FALSE", " 0 ")] string untruthyValue)
        {

            var metadata = new Metadata()
            {
                Items = new[]
                {
                    new Metadata.ItemsData()
                    {
                        Key = "flag",
                        Value = untruthyValue
                    }
                }
            };

            Assert.IsFalse(metadata.GetFlag("flag"));
        }

        [Test]
        public void GetFlag_WhenValueIsJunk_ThenReturnsNull(
            [Values(null, "", "junk")] string untruthyValue)
        {

            var metadata = new Metadata()
            {
                Items = new[]
                {
                    new Metadata.ItemsData()
                    {
                        Key = "flag",
                        Value = untruthyValue
                    }
                }
            };

            Assert.IsNull(metadata.GetFlag("flag"));
        }

        //---------------------------------------------------------------------
        // AsString
        //---------------------------------------------------------------------

        [Test]
        public void AsString_WhenMetadataIsEmpty_ThenReturnsEmptyList()
        {
            var metadata = new Metadata();

            Assert.AreEqual("[]", metadata.AsString());
        }

        [Test]
        public void AsString_WhenMetadataContainsEntries_ThenReturnsList()
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
