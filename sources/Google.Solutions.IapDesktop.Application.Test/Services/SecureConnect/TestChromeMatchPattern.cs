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
    public class TestChromeMatchPattern : ApplicationFixtureBase
    {
        [Test]
        public void WhenPatternIsMalformed_ThenParseThrowsException()
        {
            Assert.Throws<ArgumentException>(
                () => ChromeMatchPattern.Parse(":"));
            Assert.Throws<ArgumentException>(
                () => ChromeMatchPattern.Parse("a:"));
            Assert.Throws<ArgumentException>(
                () => ChromeMatchPattern.Parse(":host"));
            Assert.Throws<ArgumentException>(
                () => ChromeMatchPattern.Parse("not/a/proper/url/*"));
        }

        [Test]
        public void WhenPatternIsHttpUrl_ThenOnlyExactUrlsMatch()
        {
            var pattern = ChromeMatchPattern.Parse("http://example.org/foo/bar.html");

            Assert.IsTrue(pattern.IsMatch("http://example.org/foo/bar.html"));
            Assert.IsTrue(pattern.IsMatch("http://example.org/foo/bar.html"));

            Assert.IsFalse(pattern.IsMatch("http://example.org/foo/bar.html?a=b"));
        }

        [Test]
        public void WhenPatternIsHttpUrlAndHostAndPathAreWildcards_ThenSomeUrlsMatch()
        {
            var pattern = ChromeMatchPattern.Parse("http://*/*");

            Assert.IsTrue(pattern.IsMatch("http://www.google.com/"));
            Assert.IsTrue(pattern.IsMatch("http://example.org/foo/bar.html"));

            Assert.IsTrue(pattern.IsMatch("HTTP://WWW.GOOGLE.COM/"));
            Assert.IsTrue(pattern.IsMatch("HTTP://EXAMPLE.ORG/FOO/BAR.HTML"));

            Assert.IsFalse(pattern.IsMatch("https://www.google.com/"));
        }

        [Test]
        public void WhenPatternIsHttpUrlAndPathEmbedsWildcard_ThenSomeUrlsMatch()
        {
            var pattern = ChromeMatchPattern.Parse("http://*/foo*");
            
            Assert.IsTrue(pattern.IsMatch("http://example.com/foo/bar.html"));
            Assert.IsTrue(pattern.IsMatch("http://www.google.com/foo"));

            Assert.IsTrue(pattern.IsMatch("HTTP://EXAMPLE.COM/FOO/BAR.HTML"));
            Assert.IsTrue(pattern.IsMatch("HTTP://WWW.GOOGLE.COM/FOO"));
            
            Assert.IsFalse(pattern.IsMatch("http://www.google.com/bar"));
        }

        [Test]
        public void WhenPatternIsHttpUrlAndHostAndPathEmbedWildcards_ThenSomeUrlsMatch()
        {
            var pattern = ChromeMatchPattern.Parse("https://*.google.com/foo*bar");

            Assert.IsTrue(pattern.IsMatch("https://www.google.com/foo/baz/bar"));
            Assert.IsTrue(pattern.IsMatch("https://docs.google.com/foobar"));

            Assert.IsTrue(pattern.IsMatch("HTTPS://WWW.GOOGLE.COM/FOO/BAZ/BAR"));
            Assert.IsTrue(pattern.IsMatch("HTTPS://DOCS.GOOGLE.COM/FOOBAR"));

            Assert.IsFalse(pattern.IsMatch("https://google.com/foobar"));
            Assert.IsFalse(pattern.IsMatch("https://docs.google.com/foo"));
        }

        [Test]
        public void WhenPatternIsFileUrlWithEmptyHostAndWildcardInPath_ThenSomeUrlsMatch()
        {
            var pattern = ChromeMatchPattern.Parse("file:///foo*");

            Assert.IsTrue(pattern.IsMatch("file:///foo/bar.html"));
            Assert.IsTrue(pattern.IsMatch("file:///foo"));

            Assert.IsTrue(pattern.IsMatch("FILE:///FOO/BAR.HTML"));
            Assert.IsTrue(pattern.IsMatch("FILE:///FOO"));

            Assert.IsFalse(pattern.IsMatch("http:///foo"));
            Assert.IsFalse(pattern.IsMatch("https://docs.google.com/foo"));
        }

        [Test]
        public void WhenPatternIsHttpUrlPathIsWildcard_ThenSomeUrlsMatch()
        {
            var pattern = ChromeMatchPattern.Parse("http://127.0.0.1/*");

            Assert.IsTrue(pattern.IsMatch("http://127.0.0.1/"));
            Assert.IsTrue(pattern.IsMatch("http://127.0.0.1/foo/bar.html"));

            Assert.IsTrue(pattern.IsMatch("HTTP://127.0.0.1/"));
            Assert.IsTrue(pattern.IsMatch("HTTP://127.0.0.1/FOO/BAR.HTML"));

            Assert.IsFalse(pattern.IsMatch("http://127.0.0.2/"));
        }

        [Test]
        public void WhenPatternHasWildcardScheme_ThenSomeUrlsMatch()
        {
            var pattern = ChromeMatchPattern.Parse("*://mail.google.com/*");

            Assert.IsTrue(pattern.IsMatch("http://mail.google.com/foo/baz/bar"));
            Assert.IsTrue(pattern.IsMatch("https://mail.google.com/foobar"));

            Assert.IsTrue(pattern.IsMatch("HTTP://MAIL.GOOGLE.COM/FOO/BAZ/BAR"));
            Assert.IsTrue(pattern.IsMatch("HTTPS://MAIL.GOOGLE.COM/FOOBAR"));

            Assert.IsFalse(pattern.IsMatch("file://mail.google.com/foobar"));
        }

        [Test]
        public void WhenPatternIsUrn_ThenSomeUrlsMatch()
        {
            var pattern = ChromeMatchPattern.Parse("urn:*");

            Assert.IsTrue(pattern.IsMatch("urn:uuid:54723bea-c94e-480e-80c8-a69846c3f582"));

            Assert.IsFalse(pattern.IsMatch("file://mail.google.com/foobar"));
        }

        [Test]
        public void WhenPatternIsAllUrls_ThenAllUrlsMatch()
        {
            var pattern = ChromeMatchPattern.Parse("<all_urls>");

            Assert.IsTrue(pattern.IsMatch("http://mail.google.com/foo/baz/bar"));
            Assert.IsTrue(pattern.IsMatch("FILE:///FOO/BAR.HTML"));
            Assert.IsTrue(pattern.IsMatch("urn:uuid:54723bea-c94e-480e-80c8-a69846c3f582"));
        }

        // TODO: Test square brackets around wildcards, IPv6 addresses
    }
}
