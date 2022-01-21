using Google.Solutions.Common.Locator;
using Google.Solutions.IapTunneling.Iap;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Test.Iap
{
    [TestFixture]
    public class TestNetworkEndpointLocator : IapFixtureBase
    {
        [Test]
        public void WhenReferencesAreEquivalent_ThenEqualsReturnsTrue()
        {
            var ref1 = new NetworkEndpointLocator("project-1", "region-1", "network-1", "inst");
            var ref2 = new NetworkEndpointLocator("project-1", "region-1", "network-1", "inst");

            Assert.IsTrue(ref1.Equals(ref2));
            Assert.IsTrue(ref1.Equals((object)ref2));
            Assert.IsTrue(ref1 == ref2);
            Assert.IsFalse(ref1 != ref2);
        }

        [Test]
        public void WhenReferencesAreSame_ThenEqualsReturnsTrue()
        {
            var ref1 = new NetworkEndpointLocator("project-1", "region-1", "network-1", "inst");
            var ref2 = ref1;

            Assert.IsTrue(ref1.Equals(ref2));
            Assert.IsTrue(ref1.Equals((object)ref2));
            Assert.IsTrue(ref1 == ref2);
            Assert.IsFalse(ref1 != ref2);
        }

        [Test]
        public void WhenReferencesAreNotEquivalent_ThenEqualsReturnsFalse()
        {
            var ref1 = new NetworkEndpointLocator("project-1", "region-1", "network-1", "inst");
            var ref2 = new NetworkEndpointLocator("project-1", "region-2", "network-1", "inst");

            Assert.IsFalse(ref1.Equals(ref2));
            Assert.IsFalse(ref1.Equals((object)ref2));
            Assert.IsFalse(ref1 == ref2);
            Assert.IsTrue(ref1 != ref2);
        }

        [Test]
        public void WhenReferencesAreOfDifferentType_ThenEqualsReturnsFalse()
        {
            var ref1 = new NetworkEndpointLocator("project-1", "region-1", "network-1", "inst");
            var ref2 = new DiskTypeLocator("project-1", "region-1", "pd-standard");

            Assert.IsFalse(ref2.Equals(ref1));
            Assert.IsFalse(ref2.Equals((object)ref1));
            Assert.IsFalse(ref1.Equals(ref2));
            Assert.IsFalse(ref1.Equals((object)ref2));
        }

        [Test]
        public void TestEqualsNull()
        {
            var ref1 = new NetworkEndpointLocator("project-1", "region-1", "network-1", "inst");

            Assert.IsFalse(ref1.Equals(null));
            Assert.IsFalse(ref1.Equals((object)null));
            Assert.IsFalse(ref1 == null);
            Assert.IsFalse(null == ref1);
            Assert.IsTrue(ref1 != null);
            Assert.IsTrue(null != ref1);
        }
    }
}
