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

using Google.Solutions.Platform.Net;
using NUnit.Framework;
using System;

namespace Google.Solutions.Platform.Test.Net
{
    [TestFixture]
    public class TestChromePolicyUrlPattern
    {
        [Test]
        public void Parse_WhenPatternIsMalformed_ThenParseThrowsException()
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
        public void IsMatch_WhenFileScheme_ThenNoUrlsMatch()
        {
            Assert.That(ChromePolicyUrlPattern.Parse("file://path").IsMatch("file://path"), Is.False);
            Assert.That(ChromePolicyUrlPattern.Parse("file://path/sub").IsMatch("file://path/sub"), Is.False);
            Assert.That(ChromePolicyUrlPattern.Parse("file://path/sub").IsMatch("http://example.com/"), Is.False);
        }

        //---------------------------------------------------------------------
        // Wildcard.
        //---------------------------------------------------------------------

        [Test]
        public void IsMatch_WhenWildcard_ThenFileUrlsMatch()
        {
            Assert.That(ChromePolicyUrlPattern.Parse("*").IsMatch("file://path"), Is.False);
            Assert.That(ChromePolicyUrlPattern.Parse("*://path").IsMatch("file://path"), Is.False);
            Assert.That(ChromePolicyUrlPattern.Parse("file://path").IsMatch("file://path"), Is.False);
            Assert.That(ChromePolicyUrlPattern.Parse("file://path/sub").IsMatch("file://path/sub"), Is.False);
            Assert.That(ChromePolicyUrlPattern.Parse("file://path/sub").IsMatch("http://example.com/"), Is.False);
        }

        [Test]
        public void IsMatch_WhenWildcard_ThenAllHttpUrlsMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("*").IsMatch("http://example.com"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("*").IsMatch("http://example.com/path"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("*").IsMatch("http://example.com:8080/path"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("*").IsMatch("http://sub.example.com:8080/path"));
        }

        [Test]
        public void IsMatch_WhenWildcard_ThenAllHttpsUrlsMatch()
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
        public void IsMatch_WhenSchemeMissing_ThenHttpAndHttpsUrlsMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("example.com").IsMatch("http://example.com"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("example.com").IsMatch("https://example.com"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("example.com:80/path").IsMatch("https://example.com:80/path"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("example.com:80/path").IsMatch("http://example.com:80/path"));

            Assert.That(ChromePolicyUrlPattern.Parse("example.com").IsMatch("file://example.com"), Is.False);
            Assert.That(ChromePolicyUrlPattern.Parse("example.com").IsMatch("http://www.example.com"), Is.False);
            Assert.That(ChromePolicyUrlPattern.Parse("example.com").IsMatch("https://www.example.com"), Is.False);
        }

        [Test]
        public void IsMatch_WhenSchemeIsWildcard_ThenHttpAndHttpsUrlsMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("*://example.com").IsMatch("http://example.com"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("*://example.com").IsMatch("https://example.com"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("*://example.com:80/path").IsMatch("https://example.com:80/path"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("*://example.com:80/path").IsMatch("http://example.com:80/path"));

            Assert.That(ChromePolicyUrlPattern.Parse("*://example.com").IsMatch("file://example.com"), Is.False);
            Assert.That(ChromePolicyUrlPattern.Parse("*://example.com").IsMatch("http://www.example.com"), Is.False);
            Assert.That(ChromePolicyUrlPattern.Parse("*://example.com").IsMatch("https://www.example.com"), Is.False);
        }

        [Test]
        public void IsMatch_WhenSchemeIsWildcard_ThenHttpsUrlsMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("example.com").IsMatch("https://example.com"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("example.com:443/path").IsMatch("https://example.com/path"));

            Assert.That(ChromePolicyUrlPattern.Parse("example.com").IsMatch("https://www.example.com"), Is.False);
        }

        //---------------------------------------------------------------------
        // Domain matching.
        //---------------------------------------------------------------------

        [Test]
        public void IsMatch_WhenDomainHasNoWildcard_ThenOnlyExactDomainsMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://example.com").IsMatch("https://EXAMPLE.com"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://example.com").IsMatch("https://EXAMPLE.com:443"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://example.com").IsMatch("https://EXAMPLE.com/path"));

            Assert.That(ChromePolicyUrlPattern.Parse("https://example.com").IsMatch("https://www.example.com"), Is.False);
            Assert.That(ChromePolicyUrlPattern.Parse("https://example.com").IsMatch("https://eeeeexample.com"), Is.False);
        }

        [Test]
        public void IsMatch_WhenDomainIsIpv4_ThenOnlyExactDomainsMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://1.2.3.4/").IsMatch("https://1.2.3.4/"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://1.2.3.4:8000/").IsMatch("https://1.2.3.4:8000/"));

            Assert.That(ChromePolicyUrlPattern.Parse("https://1.2.3.4/").IsMatch("https://1.2.3.5/"), Is.False);
        }

        [Test]
        public void IsMatch_WhenDomainIsIpv6_ThenOnlyExactDomainsMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://[2a00:1000:4000:2::]/").IsMatch("https://[2a00:1000:4000:2::]"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://[2a00:1000:4000:2::]").IsMatch("https://[2a00:1000:4000:2::]/"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://[2a00:1000:4000:2::]:8000/").IsMatch("https://[2a00:1000:4000:2::]:8000/"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://[2a00:1000:4000:2::]/[a]").IsMatch("https://[2a00:1000:4000:2::]/[a]"));

            Assert.That(ChromePolicyUrlPattern.Parse("https://1.2.3.4/").IsMatch("https://1.2.3.5/"), Is.False);
        }

        [Test]
        public void IsMatch_WhenDomainHasWildcard_ThenExactDomainsMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://[*.]example.com").IsMatch("https://EXAMPLE.com"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://[*.]example.com").IsMatch("https://EXAMPLE.com:443"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://[*.]example.com").IsMatch("https://EXAMPLE.com/path"));
        }

        [Test]
        public void IsMatch_WhenDomainHasWildcard_ThenSubDomainsMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://[*.]example.com").IsMatch("https://www.EXAMPLE.com"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://[*.]example.com").IsMatch("https://sub.sub.sub.EXAMPLE.com:443"));

            Assert.That(ChromePolicyUrlPattern.Parse("https://example.com").IsMatch("https://eeeeexample.com"), Is.False);
        }

        //---------------------------------------------------------------------
        // Port matching.
        //---------------------------------------------------------------------

        [Test]
        public void IsMatch_WhenPortSpecified_ThenOnlyExactPortsMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("http://example.com:8080/path").IsMatch("http://EXAMPLE.com:8080/path"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("http://example.com/path").IsMatch("http://EXAMPLE.com:80/path"));

            Assert.That(ChromePolicyUrlPattern.Parse("http://example.com:8080/path").IsMatch("http://EXAMPLE.com/path"), Is.False);
            Assert.That(ChromePolicyUrlPattern.Parse("http://example.com:8080/path").IsMatch("http://EXAMPLE.com:80/path"), Is.False);
            Assert.That(ChromePolicyUrlPattern.Parse("https://[2a00:1000:4000:2::]:8000/").IsMatch("https://[2a00:1000:4000:2::]:8080/"), Is.False);
        }

        [Test]
        public void IsMatch_WhenNoPortSpecified_ThenDefaultPortsMatch()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("http://example.com").IsMatch("http://EXAMPLE.com:80"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("https://example.com").IsMatch("https://EXAMPLE.com:443"));

            Assert.That(ChromePolicyUrlPattern.Parse("http://example.com/path").IsMatch("http://EXAMPLE.com:443/path"), Is.False);
            Assert.That(ChromePolicyUrlPattern.Parse("https://example.com/path").IsMatch("https://EXAMPLE.com:80/path"), Is.False);
        }

        [Test]
        public void IsMatch_WhenPortIsWildcard_ThenAllPortssMatch()
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
        public void IsMatch_WhenPathSpecified_ThenPathIsIgnored()
        {
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("http://example.com").IsMatch("http://EXAMPLE.com/foo"));
            Assert.IsTrue(ChromePolicyUrlPattern.Parse("http://example.com/foo").IsMatch("http://EXAMPLE.com"));
        }
    }
}
