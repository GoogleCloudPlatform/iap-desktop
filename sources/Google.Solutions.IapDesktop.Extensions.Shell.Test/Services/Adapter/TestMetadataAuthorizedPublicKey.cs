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

using Google.Solutions.IapDesktop.Application.Test;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Adapter
{
    [TestFixture]
    public class TestMetadataAuthorizedPublicKey : ApplicationFixtureBase
    {
        [Test]
        public void WhenUnmanagedKeyIsInvalid_ThenParseThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => MetadataAuthorizedPublicKey.Parse(
                "xxx"));
            Assert.Throws<ArgumentException>(() => MetadataAuthorizedPublicKey.Parse(
                "login:ssh-rsa key"));
            Assert.Throws<ArgumentException>(() => MetadataAuthorizedPublicKey.Parse(
                "login: key username"));
            Assert.Throws<ArgumentException>(() => MetadataAuthorizedPublicKey.Parse(
                "login:ssh-rsa key username morejunk"));
        }

        [Test]
        public void WhenManagedKeyIsInvalid_ThenParseThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => MetadataAuthorizedPublicKey.Parse(
                "login:ssh-rsa key username google-ssh {"));
            Assert.Throws<ArgumentException>(() => MetadataAuthorizedPublicKey.Parse(
                "login:ssh-rsa key username google-ssh {}"));
            Assert.Throws<ArgumentException>(() => MetadataAuthorizedPublicKey.Parse(
                "login:ssh-rsa key username google-ssh {\"userName\": \"user\", \"expireOn\": null}"));
            Assert.Throws<ArgumentException>(() => MetadataAuthorizedPublicKey.Parse(
                "login:ssh-rsa key username google-ssh {\"userName\": \"user\", \"expireOn\": \"x\"}"));
        }

        [Test]
        public void WhenKeyIsUnmanaged_ThenParseReturnsUnmanagedKey()
        {
            var line = "login:ssh-rsa key user";
            var key = MetadataAuthorizedPublicKey.Parse(line);
            Assert.IsInstanceOf<UnmanagedMetadataAuthorizedKey>(key);

            Assert.AreEqual("login", key.LoginUsername);
            Assert.AreEqual("ssh-rsa", key.KeyType);
            Assert.AreEqual("key", key.Key);
            Assert.AreEqual("user", ((UnmanagedMetadataAuthorizedKey)key).Username);

            Assert.AreEqual(line, key.ToString());
        }

        [Test]
        public void WhenKeyIsUnmanagedButUsernameIsGoogleSsh_ThenParseReturnsUnmanagedKey()
        {
            var line = "login:ssh-rsa key google-ssh";
            var key = MetadataAuthorizedPublicKey.Parse(line);
            Assert.IsInstanceOf<UnmanagedMetadataAuthorizedKey>(key);

            Assert.AreEqual("login", key.LoginUsername);
            Assert.AreEqual("ssh-rsa", key.KeyType);
            Assert.AreEqual("key", key.Key);
            Assert.AreEqual("google-ssh", ((UnmanagedMetadataAuthorizedKey)key).Username);

            Assert.AreEqual(line, key.ToString());
        }

        [Test]
        public void WhenKeyIsManagedEcdsaKey_ThenParseReturnsManagedKey()
        {
            var line = "login:ecdsa-sha2-nistp256 AAAA google-ssh {\"userName\":" +
              "\"ldap@machine.com\",\"expireOn\":\"2015-11-01T10:43:01+0000\"}";
            var key = MetadataAuthorizedPublicKey.Parse(line);
            Assert.IsInstanceOf<ManagedMetadataAuthorizedKey>(key);

            Assert.AreEqual("login", key.LoginUsername);
            Assert.AreEqual("ecdsa-sha2-nistp256", key.KeyType);
            Assert.AreEqual("AAAA", key.Key);
            Assert.AreEqual("ldap@machine.com", ((ManagedMetadataAuthorizedKey)key).Metadata.Username);
            Assert.AreEqual(new DateTime(2015, 11, 1, 10, 43, 1, 0, DateTimeKind.Utc),
                ((ManagedMetadataAuthorizedKey)key).Metadata.ExpireOn.ToUniversalTime());

            Assert.AreEqual(line, key.ToString());
        }

        [Test]
        public void WhenKeyIsManagedRsaKey_ThenParseReturnsManagedKey()
        {
            var line = "login:ssh-rsa key google-ssh {\"userName\":\"username@example.com\"," +
                "\"expireOn\":\"2021-01-15T15:22:35+0000\"}";
            var key = MetadataAuthorizedPublicKey.Parse(line);
            Assert.IsInstanceOf<ManagedMetadataAuthorizedKey>(key);

            Assert.AreEqual("login", key.LoginUsername);
            Assert.AreEqual("ssh-rsa", key.KeyType);
            Assert.AreEqual("key", key.Key);
            Assert.AreEqual("username@example.com", ((ManagedMetadataAuthorizedKey)key).Metadata.Username);
            Assert.AreEqual(new DateTime(2021, 01, 15, 15, 22, 35, 0, DateTimeKind.Utc),
                ((ManagedMetadataAuthorizedKey)key).Metadata.ExpireOn.ToUniversalTime());

            Assert.AreEqual(line, key.ToString());
        }

        [Test]
        public void WhenKeySerialized_ThenTimestampHasNoMilliseconds()
        {
            var key = new ManagedMetadataAuthorizedKey(
                "login",
                "ssh-rsa",
                "key",
                new ManagedKeyMetadata(
                    "joe@example.com",
                    new DateTime(2020, 1, 1, 23, 59, 59, 123, DateTimeKind.Utc)));

            Assert.AreEqual(
                "login:ssh-rsa key google-ssh {\"userName\":\"joe@example.com\",\"expireOn\":\"2020-01-01T23:59:59+0000\"}",
                key.ToString());
        }
    }
}
