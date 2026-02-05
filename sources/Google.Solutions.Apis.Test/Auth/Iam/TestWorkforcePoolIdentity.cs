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

using Google.Solutions.Apis.Auth.Iam;
using NUnit.Framework;
using System;

namespace Google.Solutions.Apis.Test.Auth.Iam
{
    [TestFixture]
    public class TestWorkforcePoolIdentity
    {
        //---------------------------------------------------------------------
        // FromPrincipalIdentifier.
        //---------------------------------------------------------------------

        [Test]
        public void FromPrincipalIdentifier_WhenPrincipalIdentifierIsNullOrEmpty()
        {
            Assert.Throws<ArgumentNullException>(() => WorkforcePoolIdentity.FromPrincipalIdentifier(null!));
            Assert.Throws<ArgumentException>(() => WorkforcePoolIdentity.FromPrincipalIdentifier(string.Empty));
        }

        [Test]
        public void FromPrincipalIdentifier_WhenPrincipalIdentifierIsMalformed(
            [Values("x", "principal://", " ")] string id)
        {
            Assert.Throws<ArgumentException>(
                () => WorkforcePoolIdentity.FromPrincipalIdentifier(id));
        }

        [Test]
        public void FromPrincipalIdentifier_WhenPrincipalIdentifierComponentIsEmpty(
            [Values(
                "principal://iam.googleapis.com/locations//workforcePools/POOL/subject/SUBJECT",
                "principal://iam.googleapis.com/locations/LOCATION/workforcePools//subject/SUBJECT",
                "principal://iam.googleapis.com/locations/LOCATION/workforcePools/POOL/subject/")]
            string id)
        {
            Assert.Throws<ArgumentException>(
                () => WorkforcePoolIdentity.FromPrincipalIdentifier(id));
        }

        [Test]
        public void FromPrincipalIdentifier_WhenPrincipalIdentifierValid()
        {
            var subject = WorkforcePoolIdentity.FromPrincipalIdentifier(
                "principal://iam.googleapis.com/locations/LOCATION/workforcePools/POOL/subject/SUBJECT");

            Assert.That(subject.Location, Is.EqualTo("LOCATION"));
            Assert.That(subject.Pool, Is.EqualTo("POOL"));
            Assert.That(subject.Subject, Is.EqualTo("SUBJECT"));
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToString_ReturnsPrincipalIdentifier()
        {
            var id = "principal://iam.googleapis.com/locations/LOCATION/workforcePools/POOL/subject/SUBJECT";
            var subject = WorkforcePoolIdentity.FromPrincipalIdentifier(id);

            Assert.That(subject.ToString(), Is.EqualTo(id));
        }
    }
}
