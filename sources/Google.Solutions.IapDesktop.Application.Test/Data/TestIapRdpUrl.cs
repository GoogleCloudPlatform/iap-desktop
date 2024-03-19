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

using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.Testing.Application.Test;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Application.Test.Data
{
    [TestFixture]
    public class TestIapRdpUrl : ApplicationFixtureBase
    {
        //---------------------------------------------------------------------
        // Base URL parsing.
        //---------------------------------------------------------------------

        [Test]
        public void WhenStringIsNull_ThenFromStringThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => IapRdpUrl.FromString(null!));
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
        public void WhenTooManySlashed_ThenFromStringThrowsIapRdpUrlFormatException()
        {
            Assert.Throws<IapRdpUrlFormatException>(() => IapRdpUrl.FromString("iap-rdp:my-project/us-central1-a/my-instance/baz"));
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
            Assert.AreEqual("my-instance", url.Instance.Name);
        }

        [Test]
        public void WhenTripleSlashUsed_ThenFromStringSucceeds()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance");

            Assert.AreEqual("my-project", url.Instance.ProjectId);
            Assert.AreEqual("us-central1-a", url.Instance.Zone);
            Assert.AreEqual("my-instance", url.Instance.Name);
        }

        [Test]
        public void WhenSingleSlashUsed_ThenFromStringSucceeds()
        {
            var url = IapRdpUrl.FromString("iap-rdp:/my-project/us-central1-a/my-instance");

            Assert.AreEqual("my-project", url.Instance.ProjectId);
            Assert.AreEqual("us-central1-a", url.Instance.Zone);
            Assert.AreEqual("my-instance", url.Instance.Name);
        }

        [Test]
        public void WhenTripleSlashUsed_ThenToStringReturnsSameString()
        {
            var url = "iap-rdp:///my-project/us-central1-a/my-instance";
            Assert.AreEqual(url, IapRdpUrl.FromString(url).ToString(false));
        }

        [Test]
        public void WhenIncludeQueryIsFalse_ThenToStringStripsQuery()
        {
            var url = "iap-rdp:///my-project/us-central1-a/my-instance?a=b&c=d";
            Assert.AreEqual(
                "iap-rdp:///my-project/us-central1-a/my-instance",
                IapRdpUrl.FromString(url).ToString(false));
        }

        [Test]
        public void WhenIncludeQueryIsTrue_ThenToStringIncludesEscapedQuery()
        {
            var url = "iap-rdp:///my-project/us-central1-a/my-instance?" +
                "foo=%C2%B0!%22%C2%A7%24%25%26%2F()%3D%3F&bar=".ToLower();
            Assert.AreEqual(
                url,
                IapRdpUrl.FromString(url).ToString(true));
        }
    }
}
