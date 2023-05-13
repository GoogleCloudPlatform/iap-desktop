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

using Google.Solutions.IapDesktop.Application.Services.Auth;
using Google.Solutions.IapDesktop.Core.Auth;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.Ssh.Auth;
using Moq;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Ssh
{
    [TestFixture]
    public class TestAuthorizedKey : ShellFixtureBase
    {
        //---------------------------------------------------------------------
        // Metadata with preferred username.
        //---------------------------------------------------------------------

        [Test]
        public void WhenPreferredUsernameIsEmpty_ThenForMetadataThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(
                () => AuthorizedKeyPair.ForMetadata(
                    new Mock<ISshKeyPair>().Object,
                    "",
                    false,
                    new Mock<IAuthorization>().Object));

            Assert.Throws<ArgumentException>(
                () => AuthorizedKeyPair.ForMetadata(
                    new Mock<ISshKeyPair>().Object,
                    " ",
                    false,
                    new Mock<IAuthorization>().Object));
        }

        [Test]
        public void WhenPreferredUsernameIsInvalid_ThenForMetadataThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(
                () => AuthorizedKeyPair.ForMetadata(
                    new Mock<ISshKeyPair>().Object,
                    "!user",
                    false,
                    new Mock<IAuthorization>().Object));
        }

        [Test]
        public void WhenPreferredUsernameIsValid_ThenUsernameIsUsed()
        {
            var key = AuthorizedKeyPair.ForMetadata(
                new Mock<ISshKeyPair>().Object,
                "user",
                false,
                new Mock<IAuthorization>().Object);
            Assert.AreEqual("user", key.Username);
        }

        //---------------------------------------------------------------------
        // Metadata without preferred username.
        //---------------------------------------------------------------------

        [Test]
        public void WhenEmailValid_ThenForMetadataGeneratesUsername()
        {
            var sshKey = new Mock<ISshKeyPair>().Object;
            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Email)
                .Returns("j@ex.ample");

            var authorizedKey = AuthorizedKeyPair.ForMetadata(
                sshKey,
                null,
                true,
                authorization.Object);

            Assert.AreEqual("j", authorizedKey.Username);
            Assert.AreEqual(KeyAuthorizationMethods.InstanceMetadata, authorizedKey.AuthorizationMethod);
            Assert.AreSame(sshKey, authorizedKey.KeyPair);
        }

        [Test]
        public void WhenEmailTooLong_ThenForMetadataStripsUsername()
        {
            var sshKey = new Mock<ISshKeyPair>().Object;
            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Email)
                .Returns("ABCDEFGHIJKLMNOPQRSTUVWXYZabcxyz0@ex.ample");

            var authorizedKey = AuthorizedKeyPair.ForMetadata(
                sshKey,
                null,
                false,
                authorization.Object);

            Assert.AreEqual("abcdefghijklmnopqrstuvwxyzabcxyz", authorizedKey.Username);
            Assert.AreEqual(KeyAuthorizationMethods.ProjectMetadata, authorizedKey.AuthorizationMethod);
            Assert.AreSame(sshKey, authorizedKey.KeyPair);
        }

        [Test]
        public void WhenEmailContainsInvalidChars_ThenForMetadataReplacesChars()
        {
            var sshKey = new Mock<ISshKeyPair>().Object;
            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Email)
                .Returns("1+9@ex.ample");

            var authorizedKey = AuthorizedKeyPair.ForMetadata(
                sshKey,
                null,
                false,
                authorization.Object);

            Assert.AreEqual("g1_9", authorizedKey.Username);
            Assert.AreEqual(KeyAuthorizationMethods.ProjectMetadata, authorizedKey.AuthorizationMethod);
            Assert.AreSame(sshKey, authorizedKey.KeyPair);
        }

        [Test]
        public void WhenEmailContainsUpperCaseChars_ThenForMetadataReplacesChars()
        {
            var sshKey = new Mock<ISshKeyPair>().Object;
            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Email)
                .Returns("ABC@ex.ample");

            var authorizedKey = AuthorizedKeyPair.ForMetadata(
                sshKey,
                null,
                false,
                authorization.Object);

            Assert.AreEqual("abc", authorizedKey.Username);
            Assert.AreEqual(KeyAuthorizationMethods.ProjectMetadata, authorizedKey.AuthorizationMethod);
            Assert.AreSame(sshKey, authorizedKey.KeyPair);
        }
    }
}
