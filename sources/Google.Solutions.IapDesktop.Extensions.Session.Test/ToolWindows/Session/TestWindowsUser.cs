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
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Moq;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Session
{
    [TestFixture]
    public class TestWindowsUser
    {
        private static IOidcSession CreateSession(string email)
        {
            var session = new Mock<IOidcSession>();
            session
                .SetupGet(a => a.Username)
                .Returns(email);

            return session.Object;
        }

        //---------------------------------------------------------------------
        // IsUserPrincipalName.
        //---------------------------------------------------------------------

        public void IsUserPrincipalName_WhenNameIsValid(
            [Values("user@domain.com", "a@b.c")] string upn)
        {
            Assert.IsTrue(WindowsUser.IsUserPrincipalName(upn));
        }

        public void IsUserPrincipalNameWhenNameIsInvalid(
            [Values("user@", "@user", "a.b@c")] string upn)
        {
            Assert.IsFalse(WindowsUser.IsUserPrincipalName(upn));
        }

        //---------------------------------------------------------------------
        // IsLocalUsername.
        //---------------------------------------------------------------------

        public void IsLocalUsername_WhenNameIsValid(
            [Values("user", "u")] string name)
        {
            Assert.IsTrue(WindowsUser.IsLocalUsername(name));
        }

        public void IsLocalUsername_WhenNameIsInvalid(
            [Values("DOMAIN\\user", "u12345678901234567890", "user@domain.tld")] string name)
        {
            Assert.IsFalse(WindowsUser.IsLocalUsername(name));
        }

        //---------------------------------------------------------------------
        // SuggestUsername.
        //---------------------------------------------------------------------

        [Test]
        public void SuggestUsername_WhenEmailCompliant()
        {
            Assert.AreEqual(
                "bob.b",
                WindowsUser.SuggestUsername(
                    CreateSession("bob.b@example.com")));
        }

        [Test]
        public void SuggestUsername_WhenEmailTooLong()
        {
            Assert.AreEqual(
                "bob01234567890123456",
                WindowsUser.SuggestUsername(
                    CreateSession("bob01234567890123456789@example.com")));
        }

        [Test]
        public void SuggestUsername_WhenEmailNull()
        {
            Assert.AreEqual(
                Environment.UserName,
                WindowsUser.SuggestUsername(
                    CreateSession(null!)));
        }

        [Test]
        public void SuggestUsername_WhenEmailInvalid()
        {
            Assert.AreEqual(
                Environment.UserName,
                WindowsUser.SuggestUsername(
                    CreateSession("bob")));
        }
    }
}
