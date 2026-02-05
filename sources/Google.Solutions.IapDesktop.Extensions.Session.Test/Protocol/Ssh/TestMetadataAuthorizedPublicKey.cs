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

using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Protocol.Ssh
{
    [TestFixture]
    public class TestMetadataAuthorizedPublicKey
    {
        //---------------------------------------------------------------------
        // Parse.
        //---------------------------------------------------------------------

        [Test]
        public void Parse_WhenMalformed(
            [Values(
                "xxx",
                "login: key username",
                "login:ssh-rsa key username morejunk",
                "login:ssh-rsa key username google-ssh {",
                "login:ssh-rsa key username google-ssh {}",
                "login:ssh-rsa key username google-ssh {\"userName\": \"user\", \"expireOn\": null}",
                "login:ssh-rsa key username google-ssh {\"userName\": \"user\", \"expireOn\": \"x\"}",
                "login:ssh-rsa key username google-ssh {\"notjson}"
            )]
            string line)
        {
            Assert.IsFalse(MetadataAuthorizedPublicKey.TryParse(line, out var _));
            Assert.Throws<ArgumentException>(() => MetadataAuthorizedPublicKey.Parse(line));
        }

        [Test]
        public void Parse_UnmanagedRsaKey(
            [Values(
                "login:ssh-rsa key user",
                "login:ssh-rsa key    user",
                "  login:ssh-rsa key\tuser         "
            )]
            string line)
        {
            var key = MetadataAuthorizedPublicKey.Parse(line);
            Assert.IsInstanceOf<UnmanagedMetadataAuthorizedPublicKey>(key);

            Assert.That(key.PosixUsername, Is.EqualTo("login"));
            Assert.That(key.KeyType, Is.EqualTo("ssh-rsa"));
            Assert.That(key.PublicKey, Is.EqualTo("key"));
            Assert.That(((UnmanagedMetadataAuthorizedPublicKey)key).Email, Is.EqualTo("user"));

            Assert.That(key.ToString(), Is.EqualTo("login:ssh-rsa key user"));
        }

        [Test]
        public void Parse_UnmanagedRsaKey_WhenUsernameMissing(
            [Values(
                "login:ssh-rsa key",
                "login:ssh-rsa  key    ",
                " login:ssh-rsa \tkey\t         "
            )]
            string line)
        {
            var key = MetadataAuthorizedPublicKey.Parse(line);
            Assert.IsInstanceOf<UnmanagedMetadataAuthorizedPublicKey>(key);

            Assert.That(key.PosixUsername, Is.EqualTo("login"));
            Assert.That(key.KeyType, Is.EqualTo("ssh-rsa"));
            Assert.That(key.PublicKey, Is.EqualTo("key"));
            Assert.IsNull(((UnmanagedMetadataAuthorizedPublicKey)key).Email);

            Assert.That(
                key.ToString(), Is.EqualTo("login:ssh-rsa key"));
        }

        [Test]
        public void Parse_UnmanagedRsaKey_WhenUsernameIsGoogleSsh(
            [Values(
                "login:ssh-rsa key google-ssh",
                "login:ssh-rsa key    google-ssh",
                "login:ssh-rsa key\tgoogle-ssh         "
            )]
            string line)
        {
            var key = MetadataAuthorizedPublicKey.Parse(line);
            Assert.IsInstanceOf<UnmanagedMetadataAuthorizedPublicKey>(key);

            Assert.That(key.PosixUsername, Is.EqualTo("login"));
            Assert.That(key.KeyType, Is.EqualTo("ssh-rsa"));
            Assert.That(key.PublicKey, Is.EqualTo("key"));
            Assert.That(((UnmanagedMetadataAuthorizedPublicKey)key).Email, Is.EqualTo("google-ssh"));

            Assert.That(
                key.ToString(), Is.EqualTo("login:ssh-rsa key google-ssh"));
        }

        [Test]
        public void Parse_ManagedEcdsaKey(
            [Values(
                "login:ecdsa-sha2-nistp256 AAAA google-ssh {\"userName\":\"ldap@machine.com\",\"expireOn\":\"2015-11-01T10:43:01+0000\"}",
                "login:ecdsa-sha2-nistp256  AAAA  google-ssh {\"userName\":\"ldap@machine.com\",\"expireOn\":\"2015-11-01T10:43:01+0000\"}    ",
                "login:ecdsa-sha2-nistp256 AAAA\tgoogle-ssh\t {\"userName\":\"ldap@machine.com\",\"expireOn\":\"2015-11-01T10:43:01+0000\"}\r"
            )]
            string line)
        {
            var key = MetadataAuthorizedPublicKey.Parse(line);
            Assert.IsInstanceOf<ManagedMetadataAuthorizedPublicKey>(key);

            Assert.That(key.PosixUsername, Is.EqualTo("login"));
            Assert.That(key.KeyType, Is.EqualTo("ecdsa-sha2-nistp256"));
            Assert.That(key.PublicKey, Is.EqualTo("AAAA"));
            Assert.That(((ManagedMetadataAuthorizedPublicKey)key).Metadata.Email, Is.EqualTo("ldap@machine.com"));
            Assert.That(((ManagedMetadataAuthorizedPublicKey)key).Metadata.ExpireOn.ToUniversalTime(), Is.EqualTo(new DateTime(2015, 11, 1, 10, 43, 1, 0, DateTimeKind.Utc)));

            Assert.That(
                key.ToString(), Is.EqualTo("login:ecdsa-sha2-nistp256 AAAA google-ssh {\"userName\":\"ldap@machine.com\",\"expireOn\":\"2015-11-01T10:43:01+0000\"}"));
        }

        [Test]
        public void Parse_ManagedRsaKey(
            [Values(
                "login:ssh-rsa key google-ssh {\"userName\":\"username@example.com\",\"expireOn\":\"2021-01-15T15:22:35+0000\"}",
                " login:ssh-rsa  key  google-ssh  { \"userName\":\"username@example.com\",\"expireOn\":\"2021-01-15T15:22:35+0000\"}",
                "\tlogin:ssh-rsa key google-ssh\t\r{\"userName\":\"username@example.com\",\"expireOn\":\"2021-01-15T15:22:35+0000\"}\t"
            )]
            string line)
        {
            var key = MetadataAuthorizedPublicKey.Parse(line);
            Assert.IsInstanceOf<ManagedMetadataAuthorizedPublicKey>(key);

            Assert.That(key.PosixUsername, Is.EqualTo("login"));
            Assert.That(key.KeyType, Is.EqualTo("ssh-rsa"));
            Assert.That(key.PublicKey, Is.EqualTo("key"));
            Assert.That(((ManagedMetadataAuthorizedPublicKey)key).Metadata.Email, Is.EqualTo("username@example.com"));
            Assert.That(((ManagedMetadataAuthorizedPublicKey)key).Metadata.ExpireOn.ToUniversalTime(), Is.EqualTo(new DateTime(2021, 01, 15, 15, 22, 35, 0, DateTimeKind.Utc)));

            Assert.That(
                key.ToString(), Is.EqualTo("login:ssh-rsa key google-ssh {\"userName\":\"username@example.com\",\"expireOn\":\"2021-01-15T15:22:35+0000\"}"));
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToString_TimestampHasNoMilliseconds()
        {
            var key = new ManagedMetadataAuthorizedPublicKey(
                "login",
                "ssh-rsa",
                "key",
                new ManagedMetadataAuthorizedPublicKey.PublicKeyMetadata(
                    "joe@example.com",
                    new DateTime(2020, 1, 1, 23, 59, 59, 123, DateTimeKind.Utc)));

            Assert.That(
                key.ToString(), Is.EqualTo("login:ssh-rsa key google-ssh {\"userName\":\"joe@example.com\",\"expireOn\":\"2020-01-01T23:59:59+0000\"}"));
        }

        //---------------------------------------------------------------------
        // Equals.
        //---------------------------------------------------------------------

        [Test]
        public void Equals_WhenOtherIsNull()
        {
            var key = new UnmanagedMetadataAuthorizedPublicKey(
                "bob",
                "ssh-rsa",
                "AAAA",
                "bob@gmail.com");

            Assert.IsFalse(key.Equals((object?)null));
            Assert.IsFalse(key!.Equals((MetadataAuthorizedPublicKey?)null));
        }

        [Test]
        public void Equals_WhenKeysEquivalent()
        {
            var key1 = new UnmanagedMetadataAuthorizedPublicKey(
                "bob",
                "ssh-rsa",
                "AAAA",
                "bob@gmail.com");

            var key2 = new UnmanagedMetadataAuthorizedPublicKey(
                "bob",
                "ssh-rsa",
                "AAAA",
                "bob@gmail.com");

            Assert.IsTrue(key1.Equals(key2));
            Assert.That(key2.GetHashCode(), Is.EqualTo(key1.GetHashCode()));
        }

        [Test]
        public void Equals_WhenKeysEquivalentExceptForEmail()
        {
            var key1 = new UnmanagedMetadataAuthorizedPublicKey(
                "bob",
                "ssh-rsa",
                "AAAA",
                "bob@gmail.com");

            var key2 = new UnmanagedMetadataAuthorizedPublicKey(
                "bob",
                "ssh-rsa",
                "AAAA",
                "bob@example.com");

            Assert.IsTrue(key1.Equals(key2));
            Assert.That(key2.GetHashCode(), Is.EqualTo(key1.GetHashCode()));
        }

        [Test]
        public void Equals_WhenKeysEquivalentButOneIsManaged()
        {
            var key1 = new UnmanagedMetadataAuthorizedPublicKey(
                "bob",
                "ssh-rsa",
                "AAAA",
                "bob@gmail.com");

            var key2 = new ManagedMetadataAuthorizedPublicKey(
                "bob",
                "ssh-rsa",
                "AAAA",
                new ManagedMetadataAuthorizedPublicKey.PublicKeyMetadata(
                    "bob@example.com",
                    new DateTime(2020, 1, 1, 23, 59, 59, 123, DateTimeKind.Utc)));

            Assert.IsTrue(key2.Equals(key1));
            Assert.IsTrue(key1.Equals(key2));
        }
    }
}
