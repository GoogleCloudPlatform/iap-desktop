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

using Google.Solutions.IapDesktop.Application.Services.Auth;
using Google.Solutions.IapDesktop.Core.Auth;
using Google.Solutions.IapDesktop.Extensions.Shell.Data;
using Moq;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Data
{
    [TestFixture]
    public class TestSamAccountName
    {
        private static IAuthorization CreateAuthorization(string email)
        {
            var authorization = new Mock<IAuthorization>();
            authorization.SetupGet(a => a.Email).Returns(email);
            return authorization.Object;
        }

        //---------------------------------------------------------------------
        // SuggestFromGoogleEmailAddress.
        //---------------------------------------------------------------------

        [Test]
        public void WhenEmailCompliant_ThenSuggestFromGoogleEmailAddressReturnsName()
        {
            Assert.AreEqual(
                "bob.b",
                SamAccountName.SuggestFromGoogleEmailAddress(
                    CreateAuthorization("bob.b@example.com")));
        }

        [Test]
        public void WhenEmailTooLong_ThenSuggestFromGoogleEmailAddressReturnsName()
        {
            Assert.AreEqual(
                "bob01234567890123456",
                SamAccountName.SuggestFromGoogleEmailAddress(
                    CreateAuthorization("bob01234567890123456789@example.com")));
        }

        [Test]
        public void WhenEmailNull_ThenSuggestFromGoogleEmailAddressReturnsDefault()
        {
            Assert.AreEqual(
                Environment.UserName,
                SamAccountName.SuggestFromGoogleEmailAddress(
                    CreateAuthorization(null)));
        }

        [Test]
        public void WhenEmailInvalid_ThenSuggestFromGoogleEmailAddressReturnsDefault()
        {
            Assert.AreEqual(
                Environment.UserName,
                SamAccountName.SuggestFromGoogleEmailAddress(
                    CreateAuthorization("bob")));
        }
    }
}
