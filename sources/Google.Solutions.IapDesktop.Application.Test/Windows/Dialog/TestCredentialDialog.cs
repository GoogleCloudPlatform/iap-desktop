//
// Copyright 2022 Google LLC
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

using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Testing.Apis.Integration;
using Google.Solutions.Testing.Application.Test;
using Moq;
using NUnit.Framework;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.Windows.Dialog
{
    [TestFixture]
    public class TestCredentialDialog : ApplicationFixtureBase
    {
        //---------------------------------------------------------------------
        // LookupAuthenticationPackageId.
        //---------------------------------------------------------------------

        [Test]
        public void WhenInputValid_ThenLookupAuthenticationPackageIdReturnsId(
            [Values(
                AuthenticationPackage.Any,
                AuthenticationPackage.Kerberos,
                AuthenticationPackage.Negoriate,
                AuthenticationPackage.Ntlm)] AuthenticationPackage package)
        {
            Assert.GreaterOrEqual(CredentialDialog.LookupAuthenticationPackageId(package), 0);
        }

        //---------------------------------------------------------------------
        // LSA.
        //---------------------------------------------------------------------

        [Test]
        public void WhenPackageNameValid_ThenLookupAuthenticationPackageReturnsId()
        {
            using (var lsa = CredentialDialog.Lsa.ConnectUntrusted())
            {
                var package = lsa.LookupAuthenticationPackage("Kerberos");
                Assert.AreEqual(2, package);
            }
        }

        [Test]
        public void WhenPackageNameInvalid_ThenLookupAuthenticationPackageThrowsException()
        {
            using (var lsa = CredentialDialog.Lsa.ConnectUntrusted())
            {
                Assert.Throws<Win32Exception>(
                    () => lsa.LookupAuthenticationPackage("Invalid"));
            }
        }

        //---------------------------------------------------------------------
        // PromptForWindowsCredentials.
        //---------------------------------------------------------------------

        [RequiresInteraction]
        [Test]
        public void PromptForWindowsCredentials()
        {
            var dialog = new CredentialDialog(
                new Service<IThemeService>(new Mock<IServiceProvider>().Object));

            if (dialog.PromptForWindowsCredentials(
                null,
                "Caption",
                "Message",
                AuthenticationPackage.Any,
                out var credentials) == DialogResult.OK)
            {
                Assert.NotNull(credentials);
            }
        }

        //---------------------------------------------------------------------
        // PromptForUsername.
        //---------------------------------------------------------------------

        [RequiresInteraction]
        [Test]
        public void PromptForUsername()
        {
            var dialog = new CredentialDialog(
                new Service<IThemeService>(new Mock<IServiceProvider>().Object));

            if (dialog.PromptForUsername(
                null,
                "A very, very, very, very, very long caption",
                "A very, very, very, very, very long message ",
                out var username) == DialogResult.OK)
            {
                Assert.NotNull(username);
            }
        }
    }
}
