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
using Google.Solutions.IapDesktop.Application.Test;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter;
using NUnit.Framework;
using System;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Adapter
{
    [TestFixture]
    public class TestMetadataAuthorizedPublicKeySet : ApplicationFixtureBase
    {
        [Test]
        public void WhenMetadataIsEmpry_ThenFromMetadataThrowsArgumentException()
        {
            var metadata = new Metadata();

            CollectionAssert.IsEmpty(MetadataAuthorizedPublicKeySet.FromMetadata(metadata).Keys);
        }

        [Test]
        public void WhenMetadataContainsDifferentKeys_ThenFromMetadataThrowsArgumentException()
        {
            var metadata = new Metadata()
            {
                Items = new[]
                {
                    new Metadata.ItemsData()
                    {
                        Key = "foo"
                    }
                }
            };

            CollectionAssert.IsEmpty(MetadataAuthorizedPublicKeySet.FromMetadata(metadata).Keys);
        }

        [Test]
        public void WhenMetadataItemHasWrongKey_ThenFromMetadataThrowsArgumentException()
        {
            var metadata = new Metadata.ItemsData()
            {
                Key = MetadataAuthorizedPublicKeySet.LegacyMetadataKey,
                Value = " "
            };

            Assert.Throws<ArgumentException>(
                () => MetadataAuthorizedPublicKeySet.FromMetadata(metadata));
        }

        [Test]
        public void WhenMetadataItemContainsJunk_ThenFromMetadataThrowsArgumentException()
        {
            var metadata = new Metadata.ItemsData()
            {
                Key = MetadataAuthorizedPublicKeySet.MetadataKey,
                Value = "junk junk junk "
            };

            Assert.Throws<ArgumentException>(
                () => MetadataAuthorizedPublicKeySet.FromMetadata(metadata));
        }

        [Test]
        public void WhenMetadataItemIsEmpty_ThenKeySetIsEmpty()
        {
            var metadata = new Metadata.ItemsData()
            {
                Key = MetadataAuthorizedPublicKeySet.MetadataKey,
                Value = " "
            };

            CollectionAssert.IsEmpty(MetadataAuthorizedPublicKeySet.FromMetadata(metadata).Keys);
        }

        [Test]
        public void WhenMetadataItemContainsData_ThenKeySetIsPopulated()
        {
            var metadata = new Metadata.ItemsData()
            {
                Key = MetadataAuthorizedPublicKeySet.MetadataKey,
                Value = "alice:ssh-rsa key alice\n" +
                    "bob:ssh-rsa key google-ssh {\"userName\":\"bob@example.com\",\"expireOn\":\"2050-01-15T15:22:35Z\"}"
            };

            var keySet = MetadataAuthorizedPublicKeySet.FromMetadata(metadata);
            Assert.AreEqual(2, keySet.Keys.Count());
        }

        [Test]
        public void WhenMetadataItemContainsUnnecessaryWhitespace_ThenKeySetIsPopulated()
        {
            var metadata = new Metadata.ItemsData()
            {
                Key = MetadataAuthorizedPublicKeySet.MetadataKey,
                Value =
                    "alice:ssh-rsa key alice\r\n" +
                    "bob:ssh-rsa key google-ssh {\"userName\":\"bob@example.com\",\"expireOn\":\"2050-01-15T15:22:35Z\"}\n" +
                    "\n" +
                    " carol:ssh-rsa key carol \t\r\n" +
                    "dave:ssh-rsa key dave\r\n"
            };

            var keySet = MetadataAuthorizedPublicKeySet.FromMetadata(metadata);
            Assert.AreEqual(4, keySet.Keys.Count());
        }

        [Test]
        public void WhenAddingDuplicateKey_ThenAddReturnsThis()
        {
            var metadata = new Metadata.ItemsData()
            {
                Key = MetadataAuthorizedPublicKeySet.MetadataKey,
                Value = "alice:ssh-rsa key alice"
            };

            var keySet = MetadataAuthorizedPublicKeySet.FromMetadata(metadata);

            Assert.AreSame(
                keySet,
                keySet.Add(MetadataAuthorizedPublicKey.Parse("alice:ssh-rsa key notalice")));
        }

        [Test]
        public void WhenAddingNewKey_ThenAddReturnsNewSet()
        {
            var metadata = new Metadata.ItemsData()
            {
                Key = MetadataAuthorizedPublicKeySet.MetadataKey,
                Value = "alice:ssh-rsa key alice"
            };

            var keySet = MetadataAuthorizedPublicKeySet.FromMetadata(metadata)
                .Add(MetadataAuthorizedPublicKey.Parse("bob:ssh-rsa key notalice"))
                .Add(MetadataAuthorizedPublicKey.Parse("bob:ssh-rsa key2 bob"));

            Assert.AreEqual(3, keySet.Keys.Count());
        }

        [Test]
        public void WhenSetContainsEntriesWithEmptyUsername_ThenAddMaintainsEntry()
        {
            var metadata = new Metadata.ItemsData()
            {
                Key = MetadataAuthorizedPublicKeySet.MetadataKey,
                Value = $"alice:ssh-rsa key alice\n" +
                        $":ssh-rsa phantomkey2 phantom\n" +
                        $":ssh-rsa phantomkey3 google-ssh {{\"userName\":\"moe@example.com\",\"expireOn\":\"{DateTime.UtcNow.AddMinutes(1):O}\"}}\n" +
                        $"moe:ssh-rsa key2 google-ssh {{\"userName\":\"moe@example.com\",\"expireOn\":\"{DateTime.UtcNow.AddMinutes(1):O}\"}}\n"
            };

            var keySet = MetadataAuthorizedPublicKeySet.FromMetadata(metadata)
                .RemoveExpiredKeys()
                .Add(MetadataAuthorizedPublicKey.Parse("bob:ssh-rsa key2 bob"));

            Assert.AreEqual(5, keySet.Keys.Count());
            Assert.AreEqual("", keySet.Keys.First(k => k.Key == "phantomkey2").LoginUsername);
            Assert.AreEqual("", keySet.Keys.First(k => k.Key == "phantomkey3").LoginUsername);
        }

        [Test]
        public void WhenKeyExpired_ThenRemoveExpiredKeysStripsKey()
        {
            var metadata = new Metadata.ItemsData()
            {
                Key = MetadataAuthorizedPublicKeySet.MetadataKey,
                Value = $"alice:ssh-rsa key alice\n" +
                        $"joe:ssh-rsa key2 google-ssh {{\"userName\":\"joe@example.com\",\"expireOn\":\"{DateTime.UtcNow.AddMinutes(-1):O}\"}}\n" +
                        $"moe:ssh-rsa key2 google-ssh {{\"userName\":\"moe@example.com\",\"expireOn\":\"{DateTime.UtcNow.AddMinutes(1):O}\"}}\n"
            };

            var keySet = MetadataAuthorizedPublicKeySet.FromMetadata(metadata)
                .RemoveExpiredKeys();

            Assert.AreEqual(2, keySet.Keys.Count());
            Assert.IsFalse(keySet.Keys.Any(k => k.LoginUsername == "joe"));
        }
    }
}
