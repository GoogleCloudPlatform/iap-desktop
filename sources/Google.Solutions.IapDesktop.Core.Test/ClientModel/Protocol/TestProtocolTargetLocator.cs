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
using Google.Solutions.Testing.Apis;
using Moq;
using NUnit.Framework;
using System.Collections.Specialized;

namespace Google.Solutions.IapDesktop.Core.Test.ClientModel.Protocol
{
    [TestFixture]
    public class TestProtocolTargetLocator
        : EquatableFixtureBase<TestProtocolTargetLocator.TargetLocator, ProtocolTargetLocator>
    {
        private static readonly IProtocol ProtocolOne = new Mock<IProtocol>().Object;
        private static readonly IProtocol ProtocolTwo = new Mock<IProtocol>().Object;

        public class TargetLocator : ProtocolTargetLocator
        {

            public TargetLocator(
                string scheme,
                IProtocol protocol,
                ComputeEngineLocator resource,
                NameValueCollection parameters)
                : base(resource, parameters)
            {
                this.Scheme = scheme;
                this.Protocol = protocol;
            }

            public override IProtocol Protocol { get; }

            public override string Scheme { get; }

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public static bool operator ==(TargetLocator obj1, TargetLocator obj2)
            {
                if (obj1 is null)
                {
                    return obj2 is null;
                }

                return obj1.Equals(obj2);
            }

            public static bool operator !=(TargetLocator obj1, TargetLocator obj2)
            {
                return !(obj1 == obj2);
            }
        }

        protected override TargetLocator CreateInstance()
        {
            return new TargetLocator(
                "one",
                ProtocolOne,
                new InstanceLocator("project-1", "zone-1", "instance-1"),
                new NameValueCollection());
        }

        //---------------------------------------------------------------------
        // Equals.
        //---------------------------------------------------------------------

        [Test]
        public void Equals_WhenOtherHasDifferentScheme()
        {
            var locator1 = new TargetLocator(
                "one",
                ProtocolOne,
                new InstanceLocator("project-1", "zone-1", "instance-1"),
                new NameValueCollection());
            var locator2 = new TargetLocator(
                "two",
                ProtocolOne,
                new InstanceLocator("project-1", "zone-1", "instance-1"),
                new NameValueCollection());

            Assert.IsFalse(locator1.Equals(locator2));
            Assert.IsTrue(locator1 != locator2);
            Assert.AreNotEqual(locator1.GetHashCode(), locator2.GetHashCode());
        }

        [Test]
        public void Equals_WhenOtherHasDifferentProtocol()
        {
            var locator1 = new TargetLocator(
                "one",
                ProtocolOne,
                new InstanceLocator("project-1", "zone-1", "instance-1"),
                new NameValueCollection());
            var locator2 = new TargetLocator(
                "one",
                ProtocolTwo,
                new InstanceLocator("project-1", "zone-1", "instance-1"),
                new NameValueCollection());

            Assert.IsFalse(locator1.Equals(locator2));
            Assert.IsTrue(locator1 != locator2);
        }

        [Test]
        public void Equals_WhenOtherHasDifferentResource()
        {
            var locator1 = new TargetLocator(
                "one",
                ProtocolOne,
                new InstanceLocator("project-1", "zone-1", "instance-1"),
                new NameValueCollection());
            var locator2 = new TargetLocator(
                "one",
                ProtocolOne,
                new InstanceLocator("project-1", "zone-1", "instance-2"),
                new NameValueCollection());

            Assert.IsFalse(locator1.Equals(locator2));
            Assert.IsTrue(locator1 != locator2);
            Assert.AreNotEqual(locator1.GetHashCode(), locator2.GetHashCode());
        }

        [Test]
        public void Equals_WhenOtherHasDifferentParameters()
        {
            var locator1 = new TargetLocator(
                "one",
                ProtocolOne,
                new InstanceLocator("project-1", "zone-1", "instance-1"),
                new NameValueCollection());

            var parameters = new NameValueCollection
            {
                { "key", "value" }
            };
            var locator2 = new TargetLocator(
                "one",
                ProtocolOne,
                new InstanceLocator("project-1", "zone-1", "instance-1"),
                parameters);

            Assert.IsFalse(locator1.Equals(locator2));
            Assert.IsTrue(locator1 != locator2);
            Assert.AreNotEqual(locator1.GetHashCode(), locator2.GetHashCode());
        }
    }
}
