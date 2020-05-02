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

using Google.Solutions.Compute;
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Util;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Application.Test.Util
{
    [TestFixture]
    public class TestIapRdpUrl : FixtureBase
    {
        //---------------------------------------------------------------------
        // Base URL parsing.
        //---------------------------------------------------------------------

        [Test]
        public void WhenStringIsNull_ThenFromStringThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => IapRdpUrl.FromString(null));
        }

        [Test]
        public void WhenStringIsEmpty_ThenFromStringThrowsUriFormatException()
        {
            Assert.Throws<UriFormatException>(() => IapRdpUrl.FromString(string.Empty));
        }

        [Test]
        public void WhenStringIsNotAUri_ThenFromStringThrowsUriFormatException()
        {
            Assert.Throws<UriFormatException>(() => IapRdpUrl.FromString("::"));
        }

        [Test]
        public void WhenSchemeIsWrong_ThenFromStringThrowsIapRdpUrlFormatException()
        {
            Assert.Throws<IapRdpUrlFormatException>(() => IapRdpUrl.FromString("http://www/"));
        }

        [Test]
        public void WhenHostNotEmpty_ThenFromStringThrowsIapRdpUrlFormatException()
        {
            Assert.Throws<IapRdpUrlFormatException>(() => IapRdpUrl.FromString("iap-rdp://host/my-project/us-central1-a/my-instance"));
        }

        [Test]
        public void WhenLeadingSlashMissing_ThenFromStringThrowsIapRdpUrlFormatException()
        {
            Assert.Throws<IapRdpUrlFormatException>(() => IapRdpUrl.FromString("iap-rdp:my-project/us-central1-a/my-instance"));
        }

        [Test]
        public void WhenProjectIdIsIsInvalid_ThenFromStringThrowsIapRdpUrlFormatException()
        {
            Assert.Throws<IapRdpUrlFormatException>(() =>
                IapRdpUrl.FromString("iap-rdp:///__/us-central1-a/my-instance"));
        }

        [Test]
        public void WhenZoneIdIsIsInvalid_ThenFromStringThrowsIapRdpUrlFormatException()
        {
            Assert.Throws<IapRdpUrlFormatException>(() =>
                IapRdpUrl.FromString("iap-rdp:///my-project/__/my-instance"));
        }

        [Test]
        public void WhenInstanceNameIsIsInvalid_ThenFromStringThrowsIapRdpUrlFormatException()
        {
            Assert.Throws<IapRdpUrlFormatException>(() =>
                IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/__"));
        }

        [Test]
        public void WhenCapitalSchemeIsUsed_ThenFromStringSucceeds()
        {
            var url = IapRdpUrl.FromString("IaP-Rdp:///my-project/us-central1-a/my-instance");

            Assert.AreEqual("my-project", url.Instance.ProjectId);
            Assert.AreEqual("us-central1-a", url.Instance.Zone);
            Assert.AreEqual("my-instance", url.Instance.InstanceName);
            Assert.AreEqual("my-instance", url.Settings.InstanceName);
        }

        [Test]
        public void WhenTripleSlashUsed_ThenFromStringSucceeds()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance");

            Assert.AreEqual("my-project", url.Instance.ProjectId);
            Assert.AreEqual("us-central1-a", url.Instance.Zone);
            Assert.AreEqual("my-instance", url.Instance.InstanceName);
            Assert.AreEqual("my-instance", url.Settings.InstanceName);
        }

        [Test]
        public void WhenSingleSlashUsed_ThenFromStringSucceeds()
        {
            var url = IapRdpUrl.FromString("iap-rdp:/my-project/us-central1-a/my-instance");

            Assert.AreEqual("my-project", url.Instance.ProjectId);
            Assert.AreEqual("us-central1-a", url.Instance.Zone);
            Assert.AreEqual("my-instance", url.Instance.InstanceName);
            Assert.AreEqual("my-instance", url.Settings.InstanceName);
        }

        [Test]
        public void WhenTripleSlashUsed_ThenToStringReturnsSameString()
        {
            var url = "iap-rdp:///my-project/us-central1-a/my-instance";
            Assert.AreEqual(url, IapRdpUrl.FromString(url).ToString(false));
        }

        //---------------------------------------------------------------------
        // Query string parsing.
        //---------------------------------------------------------------------

        [Test]
        public void WhenQueryStringMissing_ThenSettingsUsesDefaults()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance");
            var settings = url.Settings;

            Assert.IsNull(settings.Username);
            Assert.IsNull(settings.Password);
            Assert.IsNull(settings.Domain);
            Assert.AreEqual(RdpConnectionBarState._Default, settings.ConnectionBar);
            Assert.AreEqual(RdpDesktopSize._Default, settings.DesktopSize);
            Assert.AreEqual(RdpAuthenticationLevel._Default, settings.AuthenticationLevel);
            Assert.AreEqual(RdpColorDepth._Default, settings.ColorDepth);
            Assert.AreEqual(RdpAudioMode._Default, settings.AudioMode);
            Assert.AreEqual(RdpRedirectClipboard._Default, settings.RedirectClipboard);
        }

        [Test]
        public void WhenQueryStringContainsNonsense_ThenSettingsUsesDefaults()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance?a=b&user=wrongcase&_");
            var settings = url.Settings;

            Assert.IsNull(settings.Username);
            Assert.IsNull(settings.Password);
            Assert.IsNull(settings.Domain);
            Assert.AreEqual(RdpConnectionBarState._Default, settings.ConnectionBar);
            Assert.AreEqual(RdpDesktopSize._Default, settings.DesktopSize);
            Assert.AreEqual(RdpAuthenticationLevel._Default, settings.AuthenticationLevel);
            Assert.AreEqual(RdpColorDepth._Default, settings.ColorDepth);
            Assert.AreEqual(RdpAudioMode._Default, settings.AudioMode);
            Assert.AreEqual(RdpRedirectClipboard._Default, settings.RedirectClipboard);
        }

        [Test]
        public void WhenQueryStringContainsOutOfRangeValues_ThenSettingsUsesDefaults()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance?" +
                "ConnectionBar=-1&DesktopSize=a&AuthenticationLevel=null&ColorDepth=&" +
                "AudioMode=9999&RedirectClipboard=b&RedirectClipboard=c");
            var settings = url.Settings;

            Assert.IsNull(settings.Username);
            Assert.IsNull(settings.Password);
            Assert.IsNull(settings.Domain);
            Assert.AreEqual(RdpConnectionBarState._Default, settings.ConnectionBar);
            Assert.AreEqual(RdpDesktopSize._Default, settings.DesktopSize);
            Assert.AreEqual(RdpAuthenticationLevel._Default, settings.AuthenticationLevel);
            Assert.AreEqual(RdpColorDepth._Default, settings.ColorDepth);
            Assert.AreEqual(RdpAudioMode._Default, settings.AudioMode);
            Assert.AreEqual(RdpRedirectClipboard._Default, settings.RedirectClipboard);
        }

        [Test]
        public void WhenQueryStringContainsValidUserOrDomain_ThenSettingsUseDecodedValues()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance?" +
                "userNAME=John%20Doe&PassworD=ignore&Domain=%20%20mydomain&");
            var settings = url.Settings;

            Assert.AreEqual("John Doe", settings.Username);
            Assert.IsNull(settings.Password);
            Assert.AreEqual("  mydomain", settings.Domain);
        }

        [Test]
        public void WhenQueryStringContainsValidSettings_ThenSettingsUseDecodedValues()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance?" +
                "ConnectionBar=1&DesktopSize=1&AuthenticationLevel=0&ColorDepth=2&" +
                "AudioMode=2&RedirectClipboard=0");
            var settings = url.Settings;

            Assert.AreEqual(RdpConnectionBarState.Pinned, settings.ConnectionBar);
            Assert.AreEqual(RdpDesktopSize.ScreenSize, settings.DesktopSize);
            Assert.AreEqual(RdpAuthenticationLevel.AttemptServerAuthentication, settings.AuthenticationLevel);
            Assert.AreEqual(RdpColorDepth.DeepColor, settings.ColorDepth);
            Assert.AreEqual(RdpAudioMode.DoNotPlay, settings.AudioMode);
            Assert.AreEqual(RdpRedirectClipboard.Disabled, settings.RedirectClipboard);
        }

        [Test]
        public void WhenSettingsContainsEscapableChars_ThenToStringEscapesThem()
        {
            var url = new IapRdpUrl(
                new VmInstanceReference("project-1", "us-central1-a", "instance-1"),
                new VmInstanceSettings()
                {
                    Username = "Tom & Jerry?",
                    Domain = "\"?\""
                });

            Assert.AreEqual(
                "iap-rdp:///project-1/us-central1-a/instance-1?" + 
                    "Username=Tom+%26+Jerry%3f&Domain=%22%3f%22&" +
                    "ConnectionBar=0&DesktopSize=2&AuthenticationLevel=3&ColorDepth=1&AudioMode=0&RedirectClipboard=1", 
                url.ToString());
        }

        [Test]
        public void WhenParseStringCreatedByToString_ResultIsSame()
        {
            var url = new IapRdpUrl(
                new VmInstanceReference("project-1", "us-central1-a", "instance-1"),
                new VmInstanceSettings()
                {
                    Username = "user",
                    Domain = "domain",
                    ConnectionBar = RdpConnectionBarState.Off,
                    DesktopSize = RdpDesktopSize.ScreenSize,
                    AuthenticationLevel = RdpAuthenticationLevel.RequireServerAuthentication,
                    ColorDepth = RdpColorDepth.TrueColor,
                    AudioMode = RdpAudioMode.PlayOnServer,
                    RedirectClipboard = RdpRedirectClipboard.Disabled
                });

            var copy = IapRdpUrl.FromString(url.ToString());

            Assert.AreEqual("project-1", copy.Instance.ProjectId);
            Assert.AreEqual("us-central1-a", copy.Instance.Zone);
            Assert.AreEqual("instance-1", copy.Instance.InstanceName);

            Assert.AreEqual("user", copy.Settings.Username);
            Assert.AreEqual("domain", copy.Settings.Domain);
            Assert.AreEqual(RdpConnectionBarState.Off, copy.Settings.ConnectionBar);
            Assert.AreEqual(RdpDesktopSize.ScreenSize, copy.Settings.DesktopSize);
            Assert.AreEqual(RdpAuthenticationLevel.RequireServerAuthentication, copy.Settings.AuthenticationLevel);
            Assert.AreEqual(RdpColorDepth.TrueColor, copy.Settings.ColorDepth);
            Assert.AreEqual(RdpAudioMode.PlayOnServer, copy.Settings.AudioMode);
            Assert.AreEqual(RdpRedirectClipboard.Disabled, copy.Settings.RedirectClipboard);
        }
    }
}
