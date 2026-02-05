//
// Copyright 2023 Google LLC
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
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Moq;
using NUnit.Framework;
using System.Net;

namespace Google.Solutions.IapDesktop.Core.Test.ClientModel.Transport
{
    [TestFixture]
    public class TestIapTunnelProfile
    {
        private static readonly InstanceLocator SampleInstance
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        private static readonly IPEndPoint LoopbackEndpoint
            = new IPEndPoint(IPAddress.Loopback, 8000);

        [Test]
        public void Equals_WhenReferencesAreEquivalent()
        {
            var protocol = new Mock<IProtocol>().Object;
            var policy = new Mock<ITransportPolicy>().Object;
            var ref1 = new IapTunnel.Profile(
                protocol,
                policy,
                SampleInstance,
                22,
                LoopbackEndpoint);

            var ref2 = new IapTunnel.Profile(
                protocol,
                policy,
                SampleInstance,
                22,
                LoopbackEndpoint);

            Assert.IsTrue(ref1.Equals(ref2));
            Assert.IsTrue(ref1.Equals((object)ref2));
            Assert.IsTrue(ref1 == ref2);
            Assert.IsFalse(ref1 != ref2);
            Assert.That(ref2.GetHashCode(), Is.EqualTo(ref1.GetHashCode()));
        }

        [Test]
        public void Equals_WhenReferencesAreSame()
        {
            var ref1 = new IapTunnel.Profile(
                new Mock<IProtocol>().Object,
                new Mock<ITransportPolicy>().Object,
                SampleInstance,
                22,
                LoopbackEndpoint);
            var ref2 = ref1;

            Assert.IsTrue(ref1.Equals(ref2));
            Assert.IsTrue(ref1.Equals((object)ref2));
            Assert.IsTrue(ref1 == ref2);
            Assert.IsFalse(ref1 != ref2);
            Assert.That(ref2.GetHashCode(), Is.EqualTo(ref1.GetHashCode()));
        }

        [Test]
        public void Equals_WhenPoliciesDiffer()
        {
            var protocol = new Mock<IProtocol>().Object;

            var ref1 = new IapTunnel.Profile(
                protocol,
                new Mock<ITransportPolicy>().Object,
                SampleInstance,
                22,
                LoopbackEndpoint);

            var ref2 = new IapTunnel.Profile(
                protocol,
                new Mock<ITransportPolicy>().Object,
                SampleInstance,
                22,
                LoopbackEndpoint);

            Assert.IsFalse(ref1.Equals(ref2));
            Assert.IsFalse(ref1.Equals((object)ref2));
            Assert.IsFalse(ref1 == ref2);
            Assert.IsTrue(ref1 != ref2);
            Assert.AreNotEqual(ref1.GetHashCode(), ref2.GetHashCode());
        }

        [Test]
        public void Equals_WhenProtocolsDiffer()
        {
            var policy = new Mock<ITransportPolicy>().Object;

            var ref1 = new IapTunnel.Profile(
                new Mock<IProtocol>().Object,
                policy,
                SampleInstance,
                22,
                LoopbackEndpoint);

            var ref2 = new IapTunnel.Profile(
                new Mock<IProtocol>().Object,
                policy,
                SampleInstance,
                22,
                LoopbackEndpoint);

            Assert.IsFalse(ref1.Equals(ref2));
            Assert.IsFalse(ref1.Equals((object)ref2));
            Assert.IsFalse(ref1 == ref2);
            Assert.IsTrue(ref1 != ref2);
            Assert.AreNotEqual(ref1.GetHashCode(), ref2.GetHashCode());
        }

        [Test]
        public void Equals_WhenInstancesDiffer()
        {
            var protocol = new Mock<IProtocol>().Object;
            var policy = new Mock<ITransportPolicy>().Object;

            var ref1 = new IapTunnel.Profile(
                protocol,
                policy,
                SampleInstance,
                22,
                LoopbackEndpoint);

            var ref2 = new IapTunnel.Profile(
                protocol,
                policy,
                new InstanceLocator("project-1", "zone-1", "instance-2"),
                22,
                LoopbackEndpoint);

            Assert.IsFalse(ref1.Equals(ref2));
            Assert.IsFalse(ref1.Equals((object)ref2));
            Assert.IsFalse(ref1 == ref2);
            Assert.IsTrue(ref1 != ref2);
            Assert.AreNotEqual(ref1.GetHashCode(), ref2.GetHashCode());
        }

        [Test]
        public void Equals_WhenPortsDiffer()
        {
            var protocol = new Mock<IProtocol>().Object;
            var policy = new Mock<ITransportPolicy>().Object;

            var ref1 = new IapTunnel.Profile(
                protocol,
                policy,
                SampleInstance,
                22,
                LoopbackEndpoint);

            var ref2 = new IapTunnel.Profile(
                protocol,
                policy,
                SampleInstance,
                80,
                LoopbackEndpoint);

            Assert.IsFalse(ref1.Equals(ref2));
            Assert.IsFalse(ref1.Equals((object)ref2));
            Assert.IsFalse(ref1 == ref2);
            Assert.IsTrue(ref1 != ref2);
            Assert.AreNotEqual(ref1.GetHashCode(), ref2.GetHashCode());
        }

        [Test]
        public void Equals_WhenLocalEndpointsDiffer()
        {
            var protocol = new Mock<IProtocol>().Object;
            var policy = new Mock<ITransportPolicy>().Object;

            var ref1 = new IapTunnel.Profile(
                protocol,
                policy,
                SampleInstance,
                22,
                LoopbackEndpoint);

            var ref2 = new IapTunnel.Profile(
                protocol,
                policy,
                SampleInstance,
                22,
                null);

            Assert.IsFalse(ref1.Equals(ref2));
            Assert.IsFalse(ref1.Equals((object)ref2));
            Assert.IsFalse(ref1 == ref2);
            Assert.IsTrue(ref1 != ref2);
            Assert.AreNotEqual(ref1.GetHashCode(), ref2.GetHashCode());
        }

        [Test]
        public void Equals_WhenNull()
        {
            var ref1 = new IapTunnel.Profile(
                new Mock<IProtocol>().Object,
                new Mock<ITransportPolicy>().Object,
                SampleInstance,
                22,
                LoopbackEndpoint);

            Assert.IsFalse(ref1.Equals(null));
            Assert.IsFalse(ref1!.Equals((object)null!));
            Assert.IsFalse(ref1 == null);
            Assert.IsFalse(null == ref1);
            Assert.IsTrue(ref1 != null);
            Assert.IsTrue(null != ref1);
        }
    }
}
