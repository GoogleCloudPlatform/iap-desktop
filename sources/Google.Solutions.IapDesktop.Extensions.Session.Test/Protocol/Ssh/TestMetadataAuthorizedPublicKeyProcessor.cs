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
using Google.Solutions.Apis.Compute;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using NUnit.Framework;
using System;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Protocol.Ssh
{
    [TestFixture]
    public class TestMetadataAuthorizedPublicKeyProcessor
    {

        //---------------------------------------------------------------------
        // AddPublicKeyToMetadata.
        //---------------------------------------------------------------------

        [Test]
        public void AddPublicKeyToMetadata_WhenMetadataHasExpiredKeys_ThenAddPublicKeyToMetadataRemovesExpiredKeys()
        {
            var expiredKey = new ManagedMetadataAuthorizedPublicKey(
                "alice-expired",
                "ssh-rsa",
                "KEY-ALICE",
                new ManagedMetadataAuthorizedPublicKey.PublicKeyMetadata(
                    "alice@example.com",
                    DateTime.Now.AddDays(-1)));

            var metadata = new Metadata();
            metadata.Add(
                MetadataAuthorizedPublicKeySet.MetadataKey,
                MetadataAuthorizedPublicKeySet
                    .FromMetadata(new Metadata())
                    .Add(expiredKey)
                    .ToString());

            MetadataAuthorizedPublicKeyProcessor.AddPublicKeyToMetadata(
                metadata,
                new ManagedMetadataAuthorizedPublicKey(
                    "alice-valid",
                    "ssh-rsa",
                    "KEY-ALICE",
                    new ManagedMetadataAuthorizedPublicKey.PublicKeyMetadata(
                        "alice@example.com",
                        DateTime.Now.AddDays(+1))));

            StringAssert.DoesNotContain("alice-expired", metadata.Items.First().Value);
            StringAssert.Contains("alice-valid", metadata.Items.First().Value);
        }

        //---------------------------------------------------------------------
        // RemovePublicKeyFromMetadata.
        //---------------------------------------------------------------------

        [Test]
        public void RemovePublicKeyFromMetadata_WhenMetadataHasExpiredKeys_ThenRemoveAuthorizedKeyLeavesThemAsIs()
        {
            var expiredKey = new ManagedMetadataAuthorizedPublicKey(
                "alice-expired",
                "ssh-rsa",
                "KEY-ALICE",
                new ManagedMetadataAuthorizedPublicKey.PublicKeyMetadata(
                    "alice@example.com",
                    DateTime.Now.AddDays(-1)));

            var validKey = new ManagedMetadataAuthorizedPublicKey(
                "alice-valid",
                "ssh-rsa",
                "KEY-ALICE",
                new ManagedMetadataAuthorizedPublicKey.PublicKeyMetadata(
                    "alice@example.com",
                    DateTime.Now.AddDays(+1)));

            var metadata = new Metadata();
            metadata.Add(
                MetadataAuthorizedPublicKeySet.MetadataKey,
                MetadataAuthorizedPublicKeySet
                    .FromMetadata(new Metadata())
                    .Add(expiredKey)
                    .Add(validKey)
                    .ToString());

            MetadataAuthorizedPublicKeyProcessor.RemovePublicKeyFromMetadata(
                metadata,
                validKey);

            StringAssert.Contains("alice-expired", metadata.Items.First().Value);
        }
    }
}
