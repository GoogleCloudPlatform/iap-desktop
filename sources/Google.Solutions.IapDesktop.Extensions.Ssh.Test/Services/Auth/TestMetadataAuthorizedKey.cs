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
using Google.Solutions.IapDesktop.Extensions.Ssh.Services.Auth;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Test.Services.Auth
{
    [TestFixture]
    public class TestMetadataAuthorizedKey : ApplicationFixtureBase
    {
        [Test]
        public void WhenUnmanagedKeyIsInvalid_ThenParseThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => MetadataAuthorizedKey.Parse(
                "xxx"));
            Assert.Throws<ArgumentException>(() => MetadataAuthorizedKey.Parse(
                "login:ssh-rsa key"));
            Assert.Throws<ArgumentException>(() => MetadataAuthorizedKey.Parse(
                "login: key username"));
            Assert.Throws<ArgumentException>(() => MetadataAuthorizedKey.Parse(
                "login:ssh-rsa key username morejunk"));
        }

        [Test]
        public void WhenManagedKeyIsInvalid_ThenParseThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => MetadataAuthorizedKey.Parse(
                "login:ssh-rsa key username google-ssh {"));
            Assert.Throws<ArgumentException>(() => MetadataAuthorizedKey.Parse(
                "login:ssh-rsa key username google-ssh {}"));
            Assert.Throws<ArgumentException>(() => MetadataAuthorizedKey.Parse(
                "login:ssh-rsa key username google-ssh {\"userName\": \"user\", \"expireOn\": null}"));
            Assert.Throws<ArgumentException>(() => MetadataAuthorizedKey.Parse(
                "login:ssh-rsa key username google-ssh {\"userName\": \"user\", \"expireOn\": \"x\"}"));
        }

        [Test]
        public void WhenKeyIsUnmanaged_ThenParseReturnsUnmanagedKey()
        {
            var line = "login:ssh-rsa key user";
            var key = MetadataAuthorizedKey.Parse(line);
            Assert.IsInstanceOf<UnmanagedMetadataAuthorizedKey>(key);

            Assert.AreEqual("login", key.LoginUsername);
            Assert.AreEqual("ssh-rsa", key.KeyType);
            Assert.AreEqual("key", key.Key);
            Assert.AreEqual("user", ((UnmanagedMetadataAuthorizedKey)key).Username);

            Assert.AreEqual(line, key.ToString());
        }

        [Test]
        public void WhenKeyIsManaged_ThenParseReturnsManagedKey()
        {
            var line = "login:ssh-rsa key google-ssh {\"userName\":\"username@example.com\",\"expireOn\":\"2021-01-15T15:22:35Z\"}";
            var key = MetadataAuthorizedKey.Parse(line);
            Assert.IsInstanceOf<ManagedMetadataAuthorizedKey>(key);

            Assert.AreEqual("login", key.LoginUsername);
            Assert.AreEqual("ssh-rsa", key.KeyType);
            Assert.AreEqual("key", key.Key);
            Assert.AreEqual("username@example.com", ((ManagedMetadataAuthorizedKey)key).Metadata.Username);
            Assert.AreEqual(new DateTime(2021, 01, 15, 15, 22, 35, 0, DateTimeKind.Utc),
                ((ManagedMetadataAuthorizedKey)key).Metadata.ExpireOn.ToUniversalTime());

            Assert.AreEqual(line, key.ToString());
        }
    }
}
