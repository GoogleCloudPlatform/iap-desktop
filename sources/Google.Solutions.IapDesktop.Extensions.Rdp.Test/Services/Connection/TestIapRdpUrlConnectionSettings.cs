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

using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Extensions.Rdp.Services.Connection;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Test.Services.Connection
{
    [TestFixture]
    public class TestIapRdpUrlConnectionSettings
    {
        //---------------------------------------------------------------------
        // Query string generation.
        //---------------------------------------------------------------------

        [Test]
        public void WhenVmInstanceSettingsAreAllDefaults_ThenToUrlQueryReturnsEmptyCollection()
        {
            var settings = RdpInstanceSettings.CreateNew("pro-1", "instance-1");

            Assert.AreEqual(0, settings.ToUrlQuery().Count);
        }

        [Test]
        public void WhenVmInstanceSettingsArePopulated_ThenToUrlQueryExcludesPassword()
        {
            var settings = RdpInstanceSettings.CreateNew("pro-1", "instance-1");
            settings.Username.Value = "bob";
            settings.Password.ClearTextValue = "secret";
            settings.RedirectClipboard.EnumValue = RdpRedirectClipboard.Disabled;
            settings.ConnectionTimeout.IntValue = 123;

            var query = settings.ToUrlQuery();

            Assert.AreEqual(3, query.Count);
            Assert.AreEqual("bob", query["Username"]);
            Assert.AreEqual("0", query["RedirectClipboard"]);
            Assert.AreEqual("123", query["ConnectionTimeout"]);
        }

        [Test]
        public void WhenSettingsContainsEscapableChars_ThenToStringEscapesThem()
        {
            var settings = RdpInstanceSettings.CreateNew("project-1", "instance-1");
            settings.Username.Value = "Tom & Jerry?";
            settings.Domain.Value = "\"?\"";

            var url = new IapRdpUrl(
                new InstanceLocator("project-1", "us-central1-a", "instance-1"),
                settings.ToUrlQuery());

            Assert.AreEqual(
                "iap-rdp:///project-1/us-central1-a/instance-1?" +
                    "Username=Tom+%26+Jerry%3f&Domain=%22%3f%22",
                url.ToString());
        }

        [Test]
        public void WhenParseStringCreatedByToString_ResultIsSame()
        {
            var settings = RdpInstanceSettings.CreateNew("project-1", "instance-1");
            settings.Username.Value = "user";
            settings.Domain.Value = "domain";
            settings.ConnectionBar.Value = RdpConnectionBarState.Off;
            settings.DesktopSize.Value = RdpDesktopSize.ScreenSize;
            settings.AuthenticationLevel.Value = RdpAuthenticationLevel.RequireServerAuthentication;
            settings.ColorDepth.Value = RdpColorDepth.TrueColor;
            settings.AudioMode.Value = RdpAudioMode.PlayOnServer;
            settings.RedirectClipboard.Value = RdpRedirectClipboard.Disabled;
            settings.CredentialGenerationBehavior.Value = RdpCredentialGenerationBehavior.Disallow;

            var url = new IapRdpUrl(
                new InstanceLocator("project-1", "us-central1-a", "instance-1"),
                settings.ToUrlQuery());

            var copy = RdpInstanceSettings.FromUrl(url);

            Assert.AreEqual("user", copy.Username.Value);
            Assert.AreEqual("domain", copy.Domain.Value);
            Assert.AreEqual(RdpConnectionBarState.Off, copy.ConnectionBar.Value);
            Assert.AreEqual(RdpDesktopSize.ScreenSize, copy.DesktopSize.Value);
            Assert.AreEqual(RdpAuthenticationLevel.RequireServerAuthentication, copy.AuthenticationLevel.Value);
            Assert.AreEqual(RdpColorDepth.TrueColor, copy.ColorDepth.Value);
            Assert.AreEqual(RdpAudioMode.PlayOnServer, copy.AudioMode.Value);
            Assert.AreEqual(RdpRedirectClipboard.Disabled, copy.RedirectClipboard.Value);
            Assert.AreEqual(RdpCredentialGenerationBehavior.Disallow, copy.CredentialGenerationBehavior.Value);
        }

        //---------------------------------------------------------------------
        // Query string parsing.
        //---------------------------------------------------------------------

        [Test]
        public void WhenQueryStringMissing_ThenSettingsUsesDefaults()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance");
            var settings = RdpInstanceSettings.FromUrl(url);

            Assert.IsNull(settings.Username.Value);
            Assert.IsNull(settings.Password.Value);
            Assert.IsNull(settings.Domain.Value);
            Assert.AreEqual(RdpConnectionBarState._Default, settings.ConnectionBar.Value);
            Assert.AreEqual(RdpDesktopSize._Default, settings.DesktopSize.Value);
            Assert.AreEqual(RdpAuthenticationLevel._Default, settings.AuthenticationLevel.Value);
            Assert.AreEqual(RdpColorDepth._Default, settings.ColorDepth.Value);
            Assert.AreEqual(RdpAudioMode._Default, settings.AudioMode.Value);
            Assert.AreEqual(RdpRedirectClipboard._Default, settings.RedirectClipboard.Value);
            Assert.AreEqual(RdpCredentialGenerationBehavior._Default, settings.CredentialGenerationBehavior.Value);
        }

        [Test]
        public void WhenQueryStringContainsNonsense_ThenSettingsUsesDefaults()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance?a=b&user=wrongcase&_");
            var settings = RdpInstanceSettings.FromUrl(url);

            Assert.IsNull(settings.Username.Value);
            Assert.IsNull(settings.Password.Value);
            Assert.IsNull(settings.Domain.Value);
            Assert.AreEqual(RdpConnectionBarState._Default, settings.ConnectionBar.Value);
            Assert.AreEqual(RdpDesktopSize._Default, settings.DesktopSize.Value);
            Assert.AreEqual(RdpAuthenticationLevel._Default, settings.AuthenticationLevel.Value);
            Assert.AreEqual(RdpColorDepth._Default, settings.ColorDepth.Value);
            Assert.AreEqual(RdpAudioMode._Default, settings.AudioMode.Value);
            Assert.AreEqual(RdpRedirectClipboard._Default, settings.RedirectClipboard.Value);
            Assert.AreEqual(RdpCredentialGenerationBehavior._Default, settings.CredentialGenerationBehavior.Value);
        }

        [Test]
        public void WhenQueryStringContainsOutOfRangeValues_ThenSettingsUsesDefaults()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance?" +
                "ConnectionBar=-1&DesktopSize=a&AuthenticationLevel=null&ColorDepth=&" +
                "AudioMode=9999&RedirectClipboard=b&RedirectClipboard=c&" +
                "CredentialGenerationBehavior=-11");
            var settings = RdpInstanceSettings.FromUrl(url);

            Assert.IsNull(settings.Username.Value);
            Assert.IsNull(settings.Password.Value);
            Assert.IsNull(settings.Domain.Value);
            Assert.AreEqual(RdpConnectionBarState._Default, settings.ConnectionBar.Value);
            Assert.AreEqual(RdpDesktopSize._Default, settings.DesktopSize.Value);
            Assert.AreEqual(RdpAuthenticationLevel._Default, settings.AuthenticationLevel.Value);
            Assert.AreEqual(RdpColorDepth._Default, settings.ColorDepth.Value);
            Assert.AreEqual(RdpAudioMode._Default, settings.AudioMode.Value);
            Assert.AreEqual(RdpRedirectClipboard._Default, settings.RedirectClipboard.Value);
            Assert.AreEqual(RdpCredentialGenerationBehavior._Default, settings.CredentialGenerationBehavior.Value);
        }

        [Test]
        public void WhenQueryStringContainsValidUserOrDomain_ThenSettingsUseDecodedValues()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance?" +
                "userNAME=John%20Doe&PassworD=ignore&Domain=%20%20mydomain&");
            var settings = RdpInstanceSettings.FromUrl(url);

            Assert.AreEqual("John Doe", settings.Username.Value);
            Assert.IsNull(settings.Password.Value);
            Assert.AreEqual("  mydomain", settings.Domain.Value);
        }

        [Test]
        public void WhenQueryStringContainsValidSettings_ThenSettingsUseDecodedValues()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance?" +
                "ConnectionBar=1&DesktopSize=1&AuthenticationLevel=0&ColorDepth=2&" +
                "AudioMode=2&RedirectClipboard=0&CredentialGenerationBehavior=0&Rdpport=13389");
            var settings = RdpInstanceSettings.FromUrl(url);

            Assert.AreEqual(RdpConnectionBarState.Pinned, settings.ConnectionBar.Value);
            Assert.AreEqual(RdpDesktopSize.ScreenSize, settings.DesktopSize.Value);
            Assert.AreEqual(RdpAuthenticationLevel.AttemptServerAuthentication, settings.AuthenticationLevel.Value);
            Assert.AreEqual(RdpColorDepth.DeepColor, settings.ColorDepth.Value);
            Assert.AreEqual(RdpAudioMode.DoNotPlay, settings.AudioMode.Value);
            Assert.AreEqual(RdpRedirectClipboard.Disabled, settings.RedirectClipboard.Value);
            Assert.AreEqual(RdpCredentialGenerationBehavior.Allow, settings.CredentialGenerationBehavior.Value);
            Assert.AreEqual(13389, settings.RdpPort.Value);
        }
    }
}
