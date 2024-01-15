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

using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using NUnit.Framework;
using System.Collections.Specialized;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Settings
{
    [TestFixture]
    public class TestIapRdpUrlConnectionSettings
    {
        private static readonly InstanceLocator SampleLocator =
            new InstanceLocator("project-1", "zone-1", "instance-1");

        //---------------------------------------------------------------------
        // ToUrlQuery.
        //---------------------------------------------------------------------

        [Test]
        public void WhenVmInstanceSettingsAreAllDefaults_ThenToUrlQueryReturnsEmptyCollection()
        {
            var settings = new ConnectionSettings(SampleLocator);

            Assert.AreEqual(0, settings.ToUrlQuery().Count);
        }

        [Test]
        public void WhenVmInstanceSettingsArePopulated_ThenToUrlQueryExcludesPassword()
        {
            var settings = new ConnectionSettings(SampleLocator);
            settings.RdpUsername.Value = "bob";
            settings.RdpPassword.ClearTextValue = "secret";
            settings.RdpRedirectClipboard.EnumValue = RdpRedirectClipboard.Disabled;
            settings.RdpConnectionTimeout.IntValue = 123;

            var query = settings.ToUrlQuery();

            Assert.AreEqual(3, query.Count);
            Assert.AreEqual("bob", query["Username"]);
            Assert.AreEqual("0", query["RedirectClipboard"]);
            Assert.AreEqual("123", query["ConnectionTimeout"]);
        }

        [Test]
        public void WhenSettingsContainsEscapableChars_ThenToStringEscapesThem()
        {
            var settings = new ConnectionSettings(SampleLocator);
            settings.RdpUsername.Value = "Tom & Jerry?";
            settings.RdpDomain.Value = "\"?\"";

            var url = new IapRdpUrl(
                SampleLocator,
                settings.ToUrlQuery());

            Assert.AreEqual(
                "iap-rdp:///project-1/zone-1/instance-1?" +
                    "Username=Tom+%26+Jerry%3f&Domain=%22%3f%22",
                url.ToString());
        }

        [Test]
        public void WhenParseStringCreatedByToString_ResultIsSame()
        {
            var settings = new ConnectionSettings(SampleLocator);
            settings.RdpUsername.Value = "user";
            settings.RdpDomain.Value = "domain";
            settings.RdpConnectionBar.Value = RdpConnectionBarState.Off;
            settings.RdpAuthenticationLevel.Value = RdpAuthenticationLevel.RequireServerAuthentication;
            settings.RdpColorDepth.Value = RdpColorDepth.TrueColor;
            settings.RdpAudioMode.Value = RdpAudioMode.PlayOnServer;
            settings.RdpRedirectClipboard.Value = RdpRedirectClipboard.Disabled;

            var url = new IapRdpUrl(
                new InstanceLocator("project-1", "us-central1-a", "instance-1"),
                settings.ToUrlQuery());

            var copy = new ConnectionSettings(url);

            Assert.AreEqual("user", copy.RdpUsername.Value);
            Assert.AreEqual("domain", copy.RdpDomain.Value);
            Assert.AreEqual(RdpConnectionBarState.Off, copy.RdpConnectionBar.Value);
            Assert.AreEqual(RdpAuthenticationLevel.RequireServerAuthentication, copy.RdpAuthenticationLevel.Value);
            Assert.AreEqual(RdpColorDepth.TrueColor, copy.RdpColorDepth.Value);
            Assert.AreEqual(RdpAudioMode.PlayOnServer, copy.RdpAudioMode.Value);
            Assert.AreEqual(RdpRedirectClipboard.Disabled, copy.RdpRedirectClipboard.Value);
        }

        //---------------------------------------------------------------------
        // FromUrl.
        //---------------------------------------------------------------------

        [Test]
        public void WhenQueryStringMissing_ThenSettingsUsesDefaults()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance");
            var settings = new ConnectionSettings(url);

            Assert.IsNull(settings.RdpUsername.Value);
            Assert.IsNull(settings.RdpPassword.Value);
            Assert.IsNull(settings.RdpDomain.Value);
            Assert.AreEqual(RdpConnectionBarState._Default, settings.RdpConnectionBar.Value);
            Assert.AreEqual(RdpAuthenticationLevel._Default, settings.RdpAuthenticationLevel.Value);
            Assert.AreEqual(RdpColorDepth._Default, settings.RdpColorDepth.Value);
            Assert.AreEqual(RdpAudioMode._Default, settings.RdpAudioMode.Value);
            Assert.AreEqual(RdpRedirectClipboard._Default, settings.RdpRedirectClipboard.Value);
        }

        [Test]
        public void WhenQueryStringContainsNonsense_ThenSettingsUsesDefaults()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance?a=b&user=wrongcase&_");
            var settings = new ConnectionSettings(url);

            Assert.IsNull(settings.RdpUsername.Value);
            Assert.IsNull(settings.RdpPassword.Value);
            Assert.IsNull(settings.RdpDomain.Value);
            Assert.AreEqual(RdpConnectionBarState._Default, settings.RdpConnectionBar.Value);
            Assert.AreEqual(RdpAuthenticationLevel._Default, settings.RdpAuthenticationLevel.Value);
            Assert.AreEqual(RdpColorDepth._Default, settings.RdpColorDepth.Value);
            Assert.AreEqual(RdpAudioMode._Default, settings.RdpAudioMode.Value);
            Assert.AreEqual(RdpRedirectClipboard._Default, settings.RdpRedirectClipboard.Value);
        }

        [Test]
        public void WhenQueryStringContainsOutOfRangeValues_ThenSettingsUsesDefaults()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance?" +
                "ConnectionBar=-1&DesktopSize=a&AuthenticationLevel=null&ColorDepth=&" +
                "AudioMode=9999&RedirectClipboard=b&RedirectClipboard=c&" +
                "CredentialGenerationBehavior=-11");
            var settings = new ConnectionSettings(url);

            Assert.IsNull(settings.RdpUsername.Value);
            Assert.IsNull(settings.RdpPassword.Value);
            Assert.IsNull(settings.RdpDomain.Value);
            Assert.AreEqual(RdpConnectionBarState._Default, settings.RdpConnectionBar.Value);
            Assert.AreEqual(RdpAuthenticationLevel._Default, settings.RdpAuthenticationLevel.Value);
            Assert.AreEqual(RdpColorDepth._Default, settings.RdpColorDepth.Value);
            Assert.AreEqual(RdpAudioMode._Default, settings.RdpAudioMode.Value);
            Assert.AreEqual(RdpRedirectClipboard._Default, settings.RdpRedirectClipboard.Value);
        }

        [Test]
        public void WhenQueryStringContainsValidUserOrDomain_ThenSettingsUseDecodedValues()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance?" +
                "userNAME=John%20Doe&PassworD=ignore&Domain=%20%20mydomain&");
            var settings = new ConnectionSettings(url);

            Assert.AreEqual("John Doe", settings.RdpUsername.Value);
            Assert.IsNull(settings.RdpPassword.Value);
            Assert.AreEqual("  mydomain", settings.RdpDomain.Value);
        }

        [Test]
        public void WhenQueryStringContainsValidSettings_ThenSettingsUseDecodedValues()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance?" +
                "ConnectionBar=1&DesktopSize=1&AuthenticationLevel=0&ColorDepth=2&" +
                "AudioMode=2&RedirectClipboard=0&CredentialGenerationBehavior=0&Rdpport=13389");
            var settings = new ConnectionSettings(url);

            Assert.AreEqual(RdpConnectionBarState.Pinned, settings.RdpConnectionBar.Value);
            Assert.AreEqual(RdpAuthenticationLevel.AttemptServerAuthentication, settings.RdpAuthenticationLevel.Value);
            Assert.AreEqual(RdpColorDepth.DeepColor, settings.RdpColorDepth.Value);
            Assert.AreEqual(RdpAudioMode.DoNotPlay, settings.RdpAudioMode.Value);
            Assert.AreEqual(RdpRedirectClipboard.Disabled, settings.RdpRedirectClipboard.Value);
            Assert.AreEqual(13389, settings.RdpPort.Value);
        }

        //---------------------------------------------------------------------
        // ApplySettingsFromUrl.
        //---------------------------------------------------------------------

        [Test]
        public void WhenUrlParameterIsNullOrEmpty_ThenSettingIsLeftUnchanged(
            [Values(null, "", " ")] string emptyValue)
        {
            var queryParameters = new NameValueCollection();
            queryParameters.Add("AudioMode", emptyValue);

            var settings = new ConnectionSettings(
                new IapRdpUrl(SampleLocator, queryParameters));

            Assert.AreEqual(RdpAudioMode._Default, settings.RdpAudioMode.EnumValue);
        }

        [Test]
        public void WhenUrlParameterOutOfRange_ThenSettingIsLeftUnchanged(
            [Values("-1", "invalid", "999999999")] string invalidValue)
        {
            var queryParameters = new NameValueCollection();
            queryParameters.Add("AudioMode", invalidValue);

            var settings = new ConnectionSettings(
                new IapRdpUrl(SampleLocator, queryParameters));

            Assert.AreEqual(RdpAudioMode._Default, settings.RdpAudioMode.EnumValue);
        }

        [Test]
        public void WhenUrlParameterValid_ThenSettingIsUpdated()
        {
            var queryParameters = new NameValueCollection();
            queryParameters.Add("AudioMode", "2");

            var settings = new ConnectionSettings(
                new IapRdpUrl(SampleLocator, queryParameters));

            Assert.AreEqual(RdpAudioMode.DoNotPlay, settings.RdpAudioMode.EnumValue);
        }
    }
}
