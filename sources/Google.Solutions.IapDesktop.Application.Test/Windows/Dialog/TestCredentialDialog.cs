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
using System.Net;
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
        public void LookupAuthenticationPackageId_WhenInputValid(
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
        public void LookupAuthenticationPackage_WhenPackageNameValid()
        {
            using (var lsa = CredentialDialog.Lsa.ConnectUntrusted())
            {
                var package = lsa.LookupAuthenticationPackage("Kerberos");
                Assert.That(package, Is.EqualTo(2));
            }
        }

        [Test]
        public void LookupAuthenticationPackage_WhenPackageNameInvalid()
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
                new Service<ISystemDialogTheme>(new Mock<IServiceProvider>().Object));
            if (dialog.PromptForWindowsCredentials(
                null,
                new CredentialDialogParameters()
                {
                    Caption = "Caption",
                    Message = "Message",
                    Package = AuthenticationPackage.Any
                },
                out _,
                out var credentials) == DialogResult.OK)
            {
                Assert.NotNull(credentials);
            }
        }

        [RequiresInteraction]
        [Test]
        public void PromptForWindowsCredentials_WithSaveOption()
        {
            var dialog = new CredentialDialog(
                new Service<ISystemDialogTheme>(new Mock<IServiceProvider>().Object));
            if (dialog.PromptForWindowsCredentials(
                null,
                new CredentialDialogParameters()
                {
                    Caption = "Caption",
                    Message = "Message",
                    Package = AuthenticationPackage.Any,
                    ShowSaveCheckbox = true
                },
                out _,
                out var credentials) == DialogResult.OK)
            {
                Assert.NotNull(credentials);
            }
        }

        [RequiresInteraction]
        [Test]
        public void PromptForWindowsCredentials_WithPrefill()
        {
            var dialog = new CredentialDialog(
                new Service<ISystemDialogTheme>(new Mock<IServiceProvider>().Object));

            if (dialog.PromptForWindowsCredentials(
                null,
                new CredentialDialogParameters()
                {
                    Caption = "Caption",
                    Message = "Message",
                    Package = AuthenticationPackage.Any,
                    InputCredential = new NetworkCredential("bob@example.com", "password")
                },
                out _,
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
                new Service<ISystemDialogTheme>(new Mock<IServiceProvider>().Object));

            if (dialog.PromptForUsername(
                null,
                "A very, very, very, very, very long caption",
                "A very, very, very, very, very long message ",
                out var username) == DialogResult.OK)
            {
                Assert.NotNull(username);
            }
        }

        //---------------------------------------------------------------------
        // PackedCredential.
        //---------------------------------------------------------------------

        [Test]
        public void PackCredential_WhenDomainEmpty(
            [Values("user", "domain\\user", "user@domain")] string user)
        {
            var empty = new NetworkCredential(user, "password");
            using (var packed = new CredentialDialog.PackedCredential(empty))
            {
                Assert.That(packed.Handle, Is.Not.Null);
                Assert.That(packed.Size > 0, Is.True);

                var unpacked = packed.Unpack();

                Assert.That(unpacked.UserName, Is.EqualTo(user));
                Assert.That(unpacked.Domain, Is.EqualTo(""));
                Assert.That(unpacked.Password, Is.EqualTo("password"));
            }
        }

        [Test]
        public void PackCredential_WhenUsernameContainsDomainAndDomainNotEmpty(
            [Values("domain\\user", "user@domain")] string user)
        {
            var empty = new NetworkCredential(user, "password", "otherdomain");
            using (var packed = new CredentialDialog.PackedCredential(empty))
            {
                Assert.That(packed.Handle, Is.Not.Null);
                Assert.That(packed.Size > 0, Is.True);

                var unpacked = packed.Unpack();

                Assert.That(unpacked.UserName, Is.EqualTo(user));
                Assert.That(unpacked.Domain, Is.EqualTo(""));
                Assert.That(unpacked.Password, Is.EqualTo("password"));
            }
        }

        [Test]
        public void PackCredential_WhenDomainNotEmpty()
        {
            var empty = new NetworkCredential("user", "password", "domain");
            using (var packed = new CredentialDialog.PackedCredential(empty))
            {
                Assert.That(packed.Handle, Is.Not.Null);
                Assert.That(packed.Size > 0, Is.True);

                var unpacked = packed.Unpack();

                Assert.That(unpacked.UserName, Is.EqualTo("domain\\user"));
                Assert.That(unpacked.Domain, Is.EqualTo(""));
                Assert.That(unpacked.Password, Is.EqualTo("password"));
            }
        }

        [Test]
        public void PackCredential_WhenCredentialEmpty()
        {
            var empty = new NetworkCredential();
            using (var packed = new CredentialDialog.PackedCredential(empty))
            {
                Assert.That(packed.Handle, Is.Not.Null);
                Assert.That(packed.Size > 0, Is.True);

                var unpacked = packed.Unpack();

                Assert.That(unpacked.UserName, Is.EqualTo(""));
                Assert.That(unpacked.Domain, Is.EqualTo(""));
                Assert.That(unpacked.Password, Is.EqualTo(""));
            }
        }
    }
}
