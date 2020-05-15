//
// Copyright 2019 Google LLC
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

using NUnit.Framework;
using System;

namespace Google.Solutions.Compute.Test
{
    [TestFixture]
    public class TestGlobalResourceReference : FixtureBase
    {
        [Test]
        public void WhenPathIsValid_FromStringReturnsObject()
        {
            var ref1 = GlobalResourceReference.FromString(
                "projects/project-1/global/images/image-1");

            Assert.AreEqual("images", ref1.ResourceType);
            Assert.AreEqual("image-1", ref1.ResourceName);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void WhenPathLacksProject_FromStringThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => GlobalResourceReference.FromString(
                "/project-1/project-1/global/images/image-1"));
        }

        [Test]
        public void WhenPathInvalid_FromStringThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => GlobalResourceReference.FromString(
                "projects/project-1/notglobal/images/image-1"));
            Assert.Throws<ArgumentException>(() => GlobalResourceReference.FromString(
                "/project-1/global/images/image-1"));
            Assert.Throws<ArgumentException>(() => GlobalResourceReference.FromString(
                "/"));
        }

        [Test]
        public void WhenReferencesAreEquivalent_ThenEqualsReturnsTrue()
        {
            var ref1 = new GlobalResourceReference("proj", "images", "inst");
            var ref2 = new GlobalResourceReference("proj", "images", "inst");

            Assert.IsTrue(ref1.Equals(ref2));
            Assert.IsTrue(ref1.Equals((object)ref2));
            Assert.IsTrue(ref1 == ref2);
            Assert.IsFalse(ref1 != ref2);
        }

        [Test]
        public void WhenReferencesAreSame_ThenEqualsReturnsTrue()
        {
            var ref1 = new GlobalResourceReference("proj", "images", "inst");
            var ref2 = ref1;

            Assert.IsTrue(ref1.Equals(ref2));
            Assert.IsTrue(ref1.Equals((object)ref2));
            Assert.IsTrue(ref1 == ref2);
            Assert.IsFalse(ref1 != ref2);
        }

        [Test]
        public void WhenReferencesAreNotEquivalent_ThenEqualsReturnsFalse()
        {
            var ref1 = new GlobalResourceReference("proj", "images", "inst");
            var ref2 = new GlobalResourceReference("proj", "machineTypes", "inst");

            Assert.IsFalse(ref1.Equals(ref2));
            Assert.IsFalse(ref1.Equals((object)ref2));
            Assert.IsFalse(ref1 == ref2);
            Assert.IsTrue(ref1 != ref2);
        }

        [Test]
        public void TestEqualsNull()
        {
            var ref1 = new GlobalResourceReference("proj", "machineTypes", "inst");

            Assert.IsFalse(ref1.Equals(null));
            Assert.IsFalse(ref1.Equals((object)null));
            Assert.IsFalse(ref1 == null);
            Assert.IsFalse(null == ref1);
            Assert.IsTrue(ref1 != null);
            Assert.IsTrue(null != ref1);
        }
    }
}
