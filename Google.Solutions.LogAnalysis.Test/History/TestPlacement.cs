using Google.Solutions.LogAnalysis.History;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.LogAnalysis.Test.History
{
    [TestFixture]
    public class TestPlacement
    {
        [Test]
        public void WhenTwoPlacementsCloseAndNoneHasServer_ThenPlacementIsMerged()
        {
            var p1 = new Placement(
                null,
                new DateTime(2020, 1, 1, 10, 0, 0),
                new DateTime(2020, 1, 1, 11, 0, 0));
            var p2 = new Placement(
                null,
                new DateTime(2020, 1, 1, 11, 0, 50),
                new DateTime(2020, 1, 1, 12, 0, 0));

            Assert.IsTrue(p1.IsAdjacent(p2));

            var merged = p1.Merge(p2);
            Assert.AreEqual(
                new DateTime(2020, 1, 1, 10, 0, 0),
                merged.From);
            Assert.AreEqual(
                new DateTime(2020, 1, 1, 12, 0, 0),
                merged.To);
            Assert.IsNull(merged.ServerId);
        }

        [Test]
        public void WhenTwoPlacementsCloseAndOneHasServer_ThenPlacementIsMerged()
        {
            var p1 = new Placement(
                null,
                new DateTime(2020, 1, 1, 10, 0, 0),
                new DateTime(2020, 1, 1, 11, 0, 0));
            var p2 = new Placement(
                "server1",
                new DateTime(2020, 1, 1, 11, 0, 50),
                new DateTime(2020, 1, 1, 12, 0, 0));

            Assert.IsTrue(p1.IsAdjacent(p2));

            var merged = p1.Merge(p2);
            Assert.AreEqual(
                new DateTime(2020, 1, 1, 10, 0, 0),
                merged.From);
            Assert.AreEqual(
                new DateTime(2020, 1, 1, 12, 0, 0),
                merged.To);
            Assert.AreEqual("server1", merged.ServerId);
        }

        [Test]
        public void WhenTwoPlacementsCloseAndBothHaveDifferentServers_ThenPlacementIsNotMerged()
        {
            var p1 = new Placement(
                "server2",
                new DateTime(2020, 1, 1, 10, 0, 0),
                new DateTime(2020, 1, 1, 11, 0, 0));
            var p2 = new Placement(
                "server1",
                new DateTime(2020, 1, 1, 11, 0, 50),
                new DateTime(2020, 1, 1, 12, 0, 0));

            Assert.IsFalse(p1.IsAdjacent(p2));
        }

        [Test]
        public void WhenTwoPlacementsNotClose_ThenPlacementIsNotMerged()
        {
            var p1 = new Placement(
                null,
                new DateTime(2020, 1, 1, 10, 0, 0),
                new DateTime(2020, 1, 1, 11, 0, 0));
            var p2 = new Placement(
                null,
                new DateTime(2020, 1, 1, 11, 2, 0),
                new DateTime(2020, 1, 1, 12, 0, 0));

            Assert.IsFalse(p1.IsAdjacent(p2));
        }
    }
}
