//
// Copyright 2023 Google LLC
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

using Google.Solutions.Apis.Auth;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Moq;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Protocol.Ssh
{
    [TestFixture]
    public class TestLinuxUser
    {
        private static IOidcSession CreateSession(string username)
        {
            var session = new Mock<IOidcSession>();
            session.SetupGet(s => s.Username).Returns(username);
            return session.Object;
        }

        [Test]
        public void SuggestUsername_WhenSessionUsernameIsEmail()
        {
            var session = CreateSession("j@ex.ample");
            var username = LinuxUser.SuggestUsername(session);

            Assert.That(username, Is.EqualTo("j"));
            Assert.IsTrue(LinuxUser.IsValidUsername(username));
        }

        [Test]
        public void SuggestUsername_WhenSessionUsernameNotAnEmail()
        {
            var session = CreateSession("NOTANEMAILADDRESS");
            var username = LinuxUser.SuggestUsername(session);

            Assert.That(username, Is.EqualTo("notanemailaddress"));
            Assert.IsTrue(LinuxUser.IsValidUsername(username));
        }

        [Test]
        public void SuggestUsername_WhenSessionUsernameTooLong()
        {
            var session = CreateSession("ABCDEFGHIJKLMNOPQRSTUVWXYZabcxyz0@ex.ample");
            var username = LinuxUser.SuggestUsername(session);

            Assert.That(username, Is.EqualTo("abcdefghijklmnopqrstuvwxyzabcxyz"));
            Assert.IsTrue(LinuxUser.IsValidUsername(username));
        }

        [Test]
        public void SuggestUsername_WhenUsernameContainsInvalidChars()
        {
            var session = CreateSession("1+9@ex.ample");
            var username = LinuxUser.SuggestUsername(session);

            Assert.That(username, Is.EqualTo("g1_9"));
            Assert.IsTrue(LinuxUser.IsValidUsername(username));
        }

        [Test]
        public void SuggestUsername_WhenUsernameContainsUpperCaseChars()
        {
            var session = CreateSession("ABC@ex.ample");
            var username = LinuxUser.SuggestUsername(session);

            Assert.That(username, Is.EqualTo("abc"));
            Assert.IsTrue(LinuxUser.IsValidUsername(username));
        }

        [Test]
        [TestCase("SomeUser", true)]
        [TestCase("some.user", true)]
        [TestCase("some_user", true)]
        [TestCase("some-user", true)]
        [TestCase("1user", true)]
        [TestCase("-someuser", false)]
        [TestCase("some+user", false)]
        [TestCase("some@user", false)]
        [TestCase("", false)]
        [TestCase("thisusernameexceedsthemaximumsize", false)]
        public void IsValidUsername_WhenLinuxUserIsValidated(string userName, bool expectedValid)
        {
            Assert.That(expectedValid, Is.EqualTo(LinuxUser.IsValidUsername(userName)));
        }
    }
}
