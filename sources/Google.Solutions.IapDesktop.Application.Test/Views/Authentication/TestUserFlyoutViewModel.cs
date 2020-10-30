﻿//
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

using Google.Solutions.Common.Auth;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Authentication;
using Moq;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Views.Authentication
{
    [TestFixture]
    public class TestUserFlyoutViewModel : FixtureBase
    {
        [Test]
        public void WhenUserInfoIsNull_ThenManagedByIsEmpty()
        {
            var authorization = new Mock<IAuthorization>();

            var viewModel = new UserFlyoutViewModel(
                authorization.Object,
                new CloudConsoleService());

            Assert.AreEqual("", viewModel.Email);
            Assert.AreEqual("", viewModel.ManagedBy);
        }

        [Test]
        public void WhenHdIsNull_ThenManagedByIsEmpty()
        {
            var authorization = new Mock<IAuthorization>();
            authorization.SetupGet(a => a.Email)
                .Returns("bob@example.com");
            authorization.SetupGet(a => a.UserInfo)
                .Returns(new UserInfo());

            var viewModel = new UserFlyoutViewModel(
                authorization.Object,
                new CloudConsoleService());
            
            Assert.AreEqual("bob@example.com", viewModel.Email);
            Assert.AreEqual("", viewModel.ManagedBy);
        }

        [Test]
        public void WhenHdIsNotNull_ThenManagedByIsEmpty()
        {
            var authorization = new Mock<IAuthorization>();
            authorization.SetupGet(a => a.Email)
                .Returns("bob@example.com");
            authorization.SetupGet(a => a.UserInfo)
                .Returns(new UserInfo()
                {
                    HostedDomain = "example.com"
                });

            var viewModel = new UserFlyoutViewModel(
                authorization.Object,
                new CloudConsoleService());

            Assert.AreEqual("bob@example.com", viewModel.Email);
            Assert.AreEqual("(managed by example.com)", viewModel.ManagedBy);
        }
    }
}
