//
// Copyright 2021 Google LLC
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

using Google.Solutions.IapDesktop.Application.Services.SecureConnect;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services.SecureConnect
{
    [TestFixture]
    public class TestChromePolicyUrlPattern : ApplicationFixtureBase
    {
        [Test]
        public void WhenPatternIsMalformed_ThenParseThrowsException()
        {
            Assert.Throws<ArgumentException>(
                () => ChromePolicyUrlPattern.Parse("//"));
            Assert.Throws<FormatException>(
                () => ChromePolicyUrlPattern.Parse(":"));
            Assert.Throws<FormatException>(
                () => ChromePolicyUrlPattern.Parse("a:"));
            Assert.Throws<FormatException>(
                () => ChromePolicyUrlPattern.Parse(":host"));
            Assert.Throws<FormatException>(
                () => ChromePolicyUrlPattern.Parse("http://example.com:"));
            Assert.Throws<FormatException>(
                () => ChromePolicyUrlPattern.Parse("http://domain:port/"));
        }

        [Test]
        public void WhenFileScheme_ThenNoUrlsMatch()
        {
            Assert.IsFalse(ChromePolicyUrlPattern.Parse("file://path").IsMatch("file://path"));
            Assert.IsFalse(ChromePolicyUrlPattern.Parse("file://path/sub").IsMatch("file://path/sub"));
            Assert.IsFalse(ChromePolicyUrlPattern.Parse("file://path/sub").IsMatch("http://example.com/"));
        }

        //---------------------------------------------------------------------
        // Wildcard.
        //---------------------------------------------------------------------

        [Test]
        public void WhenWildcard_ThenFileUrlsMatch()
        {
            Assert.IsFalse(ChromePolicyUrlPattern.Parse("*").IsMatch("file://path"));
            Assert.IsFalse(ChromePolicyUrlPattern.Parse("*://path").IsMatch("file://path"));
            Assert.IsFalse(ChromePolicyUrlPattern.Parse("file://path").IsMatch("file://path"));
            Assert.IsFalse(ChromePolicyUrlPattern.Parse("file://path/sub").IsMatch("file://path/sub"));
            Assert.IsFalse(ChromePolicyUrlPattern.Parse("file://path/sub").IsMatch("http://example.com/"));
        }

        [Test]
        public void WhenWildcard_ThenAllHttpUrlsMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("*").IsMatch("http://example.com"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("*").IsMatch("http://example.com/path"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("*").IsMatch("http://example.com:8080/path"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("*").IsMatch("http://sub.example.com:8080/path"));
        }

        [Test]
        public void WhenWildcard_ThenAllHttpsUrlsMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("*").IsMatch("https://example.com"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("*").IsMatch("https://example.com/path"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("*").IsMatch("https://example.com:8080/path"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("*").IsMatch("https://sub.example.com:8080/path"));
        }

        //---------------------------------------------------------------------
        // Scheme matching.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSchemeMissing_ThenHttpAndHttpsUrlsMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("example.com").IsMatch("http://example.com"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("example.com").IsMatch("https://example.com"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("example.com:80/path").IsMatch("https://example.com:80/path"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("example.com:80/path").IsMatch("http://example.com:80/path"));

            Assert.IsFalse(ChromePolicyUrlPattern.Parse("example.com").IsMatch("file://example.com"));
            Assert.IsFalse(ChromePolicyUrlPattern.Parse("example.com").IsMatch("http://www.example.com"));
            Assert.IsFalse(ChromePolicyUrlPattern.Parse("example.com").IsMatch("https://www.example.com"));
        }

        [Test]
        public void WhenSchemeIsWildcard_ThenHttpAndHttpsUrlsMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("*://example.com").IsMatch("http://example.com"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("*://example.com").IsMatch("https://example.com"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("*://example.com:80/path").IsMatch("https://example.com:80/path"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("*://example.com:80/path").IsMatch("http://example.com:80/path"));

            Assert.IsFalse(ChromePolicyUrlPattern.Parse("*://example.com").IsMatch("file://example.com"));
            Assert.IsFalse(ChromePolicyUrlPattern.Parse("*://example.com").IsMatch("http://www.example.com"));
            Assert.IsFalse(ChromePolicyUrlPattern.Parse("*://example.com").IsMatch("https://www.example.com"));
        }

        [Test]
        public void WhenSchemeIsWildcard_ThenHttpsUrlsMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("example.com").IsMatch("https://example.com"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("example.com:443/path").IsMatch("https://example.com/path"));

            Assert.IsFalse(ChromePolicyUrlPattern.Parse("example.com").IsMatch("https://www.example.com"));
        }

        //---------------------------------------------------------------------
        // Domain matching.
        //---------------------------------------------------------------------

        [Test]
        public void WhenDomainHasNoWildcard_ThenOnlyExactDomainsMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://example.com").IsMatch("https://EXAMPLE.com"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://example.com").IsMatch("https://EXAMPLE.com:443"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://example.com").IsMatch("https://EXAMPLE.com/path"));

            Assert.IsFalse(ChromePolicyUrlPattern.Parse("https://example.com").IsMatch("https://www.example.com"));
            Assert.IsFalse(ChromePolicyUrlPattern.Parse("https://example.com").IsMatch("https://eeeeexample.com"));
        }

        [Test]
        public void WhenDomainIsIpv4_ThenOnlyExactDomainsMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://1.2.3.4/").IsMatch("https://1.2.3.4/"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://1.2.3.4:8000/").IsMatch("https://1.2.3.4:8000/"));

            Assert.IsFalse(ChromePolicyUrlPattern.Parse("https://1.2.3.4/").IsMatch("https://1.2.3.5/"));
        }

        [Test]
        public void WhenDomainIsIpv6_ThenOnlyExactDomainsMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://[2a00:1000:4000:2::]/").IsMatch("https://[2a00:1000:4000:2::]"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://[2a00:1000:4000:2::]").IsMatch("https://[2a00:1000:4000:2::]/"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://[2a00:1000:4000:2::]:8000/").IsMatch("https://[2a00:1000:4000:2::]:8000/"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://[2a00:1000:4000:2::]/[a]").IsMatch("https://[2a00:1000:4000:2::]/[a]"));

            Assert.IsFalse(ChromePolicyUrlPattern.Parse("https://1.2.3.4/").IsMatch("https://1.2.3.5/"));
        }

        [Test]
        public void WhenDomainHasWildcard_ThenExactDomainsMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://[*.]example.com").IsMatch("https://EXAMPLE.com"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://[*.]example.com").IsMatch("https://EXAMPLE.com:443"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://[*.]example.com").IsMatch("https://EXAMPLE.com/path"));
        }

        [Test]
        public void WhenDomainHasWildcard_ThenSubDomainsMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://[*.]example.com").IsMatch("https://www.EXAMPLE.com"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://[*.]example.com").IsMatch("https://sub.sub.sub.EXAMPLE.com:443"));

            Assert.IsFalse(ChromePolicyUrlPattern.Parse("https://example.com").IsMatch("https://eeeeexample.com"));
        }

        //---------------------------------------------------------------------
        // Port matching.
        //---------------------------------------------------------------------

        [Test]
        public void WhenPortSpecified_ThenOnlyExactPortsMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("http://example.com:8080/path").IsMatch("http://EXAMPLE.com:8080/path"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("http://example.com/path").IsMatch("http://EXAMPLE.com:80/path"));

            Assert.IsFalse(ChromePolicyUrlPattern.Parse("http://example.com:8080/path").IsMatch("http://EXAMPLE.com/path"));
            Assert.IsFalse(ChromePolicyUrlPattern.Parse("http://example.com:8080/path").IsMatch("http://EXAMPLE.com:80/path"));
            Assert.IsFalse(ChromePolicyUrlPattern.Parse("https://[2a00:1000:4000:2::]:8000/").IsMatch("https://[2a00:1000:4000:2::]:8080/"));
        }

        [Test]
        public void WhenNoPortSpecified_ThenDefaultPortsMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("http://example.com").IsMatch("http://EXAMPLE.com:80"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://example.com").IsMatch("https://EXAMPLE.com:443"));

            Assert.IsFalse(ChromePolicyUrlPattern.Parse("http://example.com/path").IsMatch("http://EXAMPLE.com:443/path"));
            Assert.IsFalse(ChromePolicyUrlPattern.Parse("https://example.com/path").IsMatch("https://EXAMPLE.com:80/path"));
        }

        [Test]
        public void WhenPortIsWildcard_ThenAllPortssMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("http://example.com:*").IsMatch("http://EXAMPLE.com:80"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("http://example.com:*").IsMatch("http://EXAMPLE.com:8080"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("http://example.com:*/").IsMatch("http://EXAMPLE.com:80"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("http://example.com:*/foo").IsMatch("http://EXAMPLE.com:8080"));
        }

        //---------------------------------------------------------------------
        // Path matching.
        //---------------------------------------------------------------------

        [Test]
        public void WhenPathSpecified_ThenPathIsIgnored()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("http://example.com").IsMatch("http://EXAMPLE.com/foo"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("http://example.com/foo").IsMatch("http://EXAMPLE.com"));
        }
    }
}
