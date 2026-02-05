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
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using NUnit.Framework;
using System;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Protocol.Ssh
{
    [TestFixture]
    public class TestMetadataAuthorizedPublicKeySet
    {
        //---------------------------------------------------------------------
        // FromMetadata.
        //---------------------------------------------------------------------

        [Test]
        public void FromMetadata_WhenMetadataIsEmpry()
        {
            var metadata = new Metadata();

            Assert.That(MetadataAuthorizedPublicKeySet.FromMetadata(metadata).Keys, Is.Empty);
        }

        [Test]
        public void FromMetadata_WhenMetadataContainsDifferentKeys()
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

            Assert.That(MetadataAuthorizedPublicKeySet.FromMetadata(metadata).Keys, Is.Empty);
        }

        [Test]
        public void FromMetadata_WhenMetadataItemHasWrongKey()
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
        public void FromMetadata_WhenMetadataItemContainsJunk()
        {
            var metadata = new Metadata.ItemsData()
            {
                Key = MetadataAuthorizedPublicKeySet.MetadataKey,
                Value = "junk junk junk "
            };

            var keySet = MetadataAuthorizedPublicKeySet.FromMetadata(metadata);
            Assert.That(keySet.Keys.Count(), Is.EqualTo(0));
            Assert.That(keySet.Items.Count(), Is.EqualTo(1));
        }

        [Test]
        public void FromMetadata_WhenMetadataItemIsEmpty()
        {
            var metadata = new Metadata.ItemsData()
            {
                Key = MetadataAuthorizedPublicKeySet.MetadataKey,
                Value = " "
            };

            Assert.That(MetadataAuthorizedPublicKeySet.FromMetadata(metadata).Keys, Is.Empty);
        }

        [Test]
        public void FromMetadata_WhenMetadataItemContainsData()
        {
            var metadata = new Metadata.ItemsData()
            {
                Key = MetadataAuthorizedPublicKeySet.MetadataKey,
                Value = "alice:ssh-rsa key alice\n" +
                    "bob:ssh-rsa key google-ssh {\"userName\":\"bob@example.com\",\"expireOn\":\"2050-01-15T15:22:35Z\"}"
            };

            var keySet = MetadataAuthorizedPublicKeySet.FromMetadata(metadata);
            Assert.That(keySet.Keys.Count(), Is.EqualTo(2));
        }

        [Test]
        public void FromMetadata_WhenMetadataItemContainsUnnecessaryWhitespace()
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
            Assert.That(keySet.Keys.Count(), Is.EqualTo(4));
        }

        //---------------------------------------------------------------------
        // Add.
        //---------------------------------------------------------------------

        [Test]
        public void Add_WhenAddingDuplicateKey()
        {
            var metadata = new Metadata.ItemsData()
            {
                Key = MetadataAuthorizedPublicKeySet.MetadataKey,
                Value = "alice:ssh-rsa key alice"
            };

            var keySet = MetadataAuthorizedPublicKeySet.FromMetadata(metadata);

            Assert.That(
                keySet.Add(MetadataAuthorizedPublicKey.Parse("alice:ssh-rsa key notalice")), Is.SameAs(keySet));
        }

        [Test]
        public void Add_WhenAddingNewKey()
        {
            var metadata = new Metadata.ItemsData()
            {
                Key = MetadataAuthorizedPublicKeySet.MetadataKey,
                Value = "alice:ssh-rsa key alice"
            };

            var keySet = MetadataAuthorizedPublicKeySet.FromMetadata(metadata)
                .Add(MetadataAuthorizedPublicKey.Parse("bob:ssh-rsa key notalice"))
                .Add(MetadataAuthorizedPublicKey.Parse("bob:ssh-rsa key2 bob"));

            Assert.That(keySet.Keys.Count(), Is.EqualTo(3));
        }

        [Test]
        public void Add_WhenSetContainsEntriesWithEmptyUsername()
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

            Assert.That(keySet.Keys.Count(), Is.EqualTo(5));
            Assert.That(keySet.Keys.First(k => k.PublicKey == "phantomkey2").PosixUsername, Is.EqualTo(""));
            Assert.That(keySet.Keys.First(k => k.PublicKey == "phantomkey3").PosixUsername, Is.EqualTo(""));
        }

        [Test]
        public void Add_WhenSetContainsUnrecognizedContent()
        {
            var metadata = new Metadata.ItemsData()
            {
                Key = MetadataAuthorizedPublicKeySet.MetadataKey,
                Value = $"alice:ssh-rsa key alice\n" +
                        $"junk\n" +
                        $"junk:ssh-rsa phantomkey3 google-ssh {{\n" +
                        $"moe:ssh-rsa key2 google-ssh {{\"userName\":\"moe@example.com\",\"expireOn\":\"{DateTime.UtcNow.AddMinutes(1):O}\"}}\n"
            };

            var keySet = MetadataAuthorizedPublicKeySet.FromMetadata(metadata)
                .RemoveExpiredKeys()
                .Add(MetadataAuthorizedPublicKey.Parse("bob:ssh-rsa key2 bob"));

            Assert.That(keySet.Keys.Count(), Is.EqualTo(3));
            Assert.That(keySet.Items.Count(), Is.EqualTo(5));
        }

        //---------------------------------------------------------------------
        // Remove.
        //---------------------------------------------------------------------

        [Test]
        public void Remove_WhenKeyNotFound()
        {
            var keySet = MetadataAuthorizedPublicKeySet.FromMetadata(new Metadata())
                .Add(MetadataAuthorizedPublicKey.Parse("alice:ssh-rsa KEY1 alice@example.com"))
                .Add(MetadataAuthorizedPublicKey.Parse("bob:ssh-rsa KEY2 bob@gmail.com"));

            var keyToRemove = new UnmanagedMetadataAuthorizedPublicKey(
                "carol",
                "ssh-rsa",
                "KEY1",
                "carol@example.com");

            var newKeySet = keySet.Remove(keyToRemove);

            Assert.That(newKeySet.Keys, Is.EquivalentTo(keySet.Keys));
        }

        [Test]
        public void Remove_WhenKeyFound()
        {
            var keySet = MetadataAuthorizedPublicKeySet.FromMetadata(new Metadata())
                .Add(MetadataAuthorizedPublicKey.Parse("alice:ssh-rsa KEY1 alice@example.com"))
                .Add(MetadataAuthorizedPublicKey.Parse("bob:ssh-rsa KEY2 bob@gmail.com"));

            var keyToRemove = new UnmanagedMetadataAuthorizedPublicKey(
                "alice",
                "ssh-rsa",
                "KEY1",
                "alice@example.com");

            var newKeySet = keySet.Remove(keyToRemove);
            Assert.That(newKeySet.Keys.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Remove_WhenSetContainsUnrecognizedContent()
        {
            var metadata = new Metadata.ItemsData()
            {
                Key = MetadataAuthorizedPublicKeySet.MetadataKey,
                Value = $"alice:ssh-rsa key alice\n" +
                        $"junk\n" +
                        $"junk:ssh-rsa phantomkey3 google-ssh {{\n" +
                        $"moe:ssh-rsa key2 google-ssh {{\"userName\":\"moe@example.com\",\"expireOn\":\"{DateTime.UtcNow.AddMinutes(1):O}\"}}\n"
            };

            var keySet = MetadataAuthorizedPublicKeySet.FromMetadata(metadata);
            var alice = keySet.Keys.First(k => k.PosixUsername == "alice");

            keySet = keySet.Remove(alice);

            Assert.That(keySet.Keys.Count(), Is.EqualTo(1));
            Assert.That(keySet.Items.Count(), Is.EqualTo(3));
        }

        //---------------------------------------------------------------------
        // RemoveExpiredKeys.
        //---------------------------------------------------------------------

        [Test]
        public void RemoveExpiredKeys_WhenKeyExpired()
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

            Assert.That(keySet.Keys.Count(), Is.EqualTo(2));
            Assert.IsFalse(keySet.Keys.Any(k => k.PosixUsername == "joe"));
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToString_WhenSetContainsUnrecognizedContent()
        {
            var metadata = new Metadata.ItemsData()
            {
                Key = MetadataAuthorizedPublicKeySet.MetadataKey,
                Value = $"alice:ssh-rsa key alice\n" +
                        $"junk\n" +
                        $"junk:ssh-rsa phantomkey3 google-ssh {{\n" +
                        $"moe:ssh-rsa key2 moe@example.com\"}}"
            };

            var keySet = MetadataAuthorizedPublicKeySet.FromMetadata(metadata);

            Assert.That(keySet.ToString(), Is.EqualTo(metadata.Value));
        }
    }
}
