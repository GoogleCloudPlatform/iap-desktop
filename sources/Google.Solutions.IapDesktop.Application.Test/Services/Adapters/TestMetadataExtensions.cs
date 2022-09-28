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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.Testing.Application.Test;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Adapters
{
    [TestFixture]
    public class TestMetadataExtensions : ApplicationFixtureBase
    {
        //---------------------------------------------------------------------
        // GetValue.
        //---------------------------------------------------------------------

        [Test]
        public void WhenMetadataIsNull_ThenGetValueReturnsNull()
        {
            var metadata = new Metadata();
            Assert.IsNull(metadata.GetValue("key"));
        }

        [Test]
        public void WhenItemNotPresent_ThenGetValueReturnsNull()
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
        public void WhenItemPresent_ThenGetValueReturnsValue()
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
        public void WhenMetadataIsNull_ThenGetFlagReturnsNull()
        {
            Assert.IsNull(MetadataExtensions.GetFlag((Metadata)null, "flag"));
        }

        [Test]
        public void WhenMetadataItemsIsNull_ThenGetFlagReturnsNull()
        {
            Assert.IsNull(new Metadata().GetFlag("flag"));
        }

        [Test]
        public void WhenValueNull_ThenGetFlagReturnsNull()
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
        public void WhenValueIsTruthy_ThenGetFlagReturnsTrue(
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
        public void WhenValueIsNotTruthy_ThenGetFlagReturnsFalse(
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
        public void WhenValueIsJunk_ThenGetFlagReturnsNull(
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
        // GetValue (Project).
        //---------------------------------------------------------------------

        [Test]
        public void WhenProjectMetadataIsNull_ThenGetFlagReturnsNull()
        {
            Assert.IsNull(MetadataExtensions.GetFlag(new Project(), "flag"));
        }

        //---------------------------------------------------------------------
        // GetValue (Instance).
        //---------------------------------------------------------------------

        [Test]
        public void WhenInstanceMetadataIsNull_ThenGetFlagReturnsNull()
        {
            Assert.IsNull(new Instance().GetFlag(new Project(), "flag"));
        }

        [Test]
        public void WhenInstanceFlagTrue_ThenGetFlagReturnsTrue()
        {
            var project = new Project();
            var instance = new Instance()
            {
                Metadata = new Metadata()
                {
                    Items = new[]
                    {
                        new Metadata.ItemsData()
                        {
                            Key = "flag",
                            Value = "true"
                        }
                    }
                }
            };

            Assert.IsTrue(instance.GetFlag(project, "flag"));
        }

        [Test]
        public void WhenProjectFlagTrueAndInstanceFlagNull_ThenGetFlagReturnsTrue()
        {
            var project = new Project()
            {
                CommonInstanceMetadata = new Metadata()
                {
                    Items = new[]
                    {
                        new Metadata.ItemsData()
                        {
                            Key = "flag",
                            Value = "true"
                        }
                    }
                }
            };
            var instance = new Instance();

            Assert.IsTrue(instance.GetFlag(project, "flag"));
        }

        [Test]
        public void WhenProjectFlagTrueAndInstanceFlagFalse_ThenGetFlagReturnsFalse()
        {
            var project = new Project()
            {
                CommonInstanceMetadata = new Metadata()
                {
                    Items = new[]
                    {
                        new Metadata.ItemsData()
                        {
                            Key = "flag",
                            Value = "true"
                        }
                    }
                }
            };
            var instance = new Instance()
            {
                Metadata = new Metadata()
                {
                    Items = new[]
                    {
                        new Metadata.ItemsData()
                        {
                            Key = "flag",
                            Value = "FALSE"
                        }
                    }
                }
            };

            Assert.IsFalse(instance.GetFlag(project, "flag"));
        }

        [Test]
        public void WhenProjectFlagFalseAndInstanceFlagTrue_ThenGetFlagReturnsTrue()
        {
            var project = new Project()
            {
                CommonInstanceMetadata = new Metadata()
                {
                    Items = new[]
                    {
                        new Metadata.ItemsData()
                        {
                            Key = "flag",
                            Value = "false"
                        }
                    }
                }
            };
            var instance = new Instance()
            {
                Metadata = new Metadata()
                {
                    Items = new[]
                    {
                        new Metadata.ItemsData()
                        {
                            Key = "flag",
                            Value = "true"
                        }
                    }
                }
            };

            Assert.IsTrue(instance.GetFlag(project, "flag"));
        }
    }
}
