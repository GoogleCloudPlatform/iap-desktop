﻿//
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

using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Session;
using Google.Solutions.Testing.Apis;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Session
{
    [TestFixture]
    public class TestSshProtocol : EquatableFixtureBase<SshProtocol, IProtocol>
    {
        protected override SshProtocol CreateInstance()
        {
            return SshProtocol.Protocol;
        }

        //---------------------------------------------------------------------
        // IsAvailable.
        //---------------------------------------------------------------------

        [Test]
        public void WhenTargetHasNoTraits_ThenIsAvailableReturnsFalse()
        {
            var target = new Mock<IProtocolTarget>();
            target
                .Setup(t => t.Traits)
                .Returns((IEnumerable<IProtocolTargetTrait>)null);

            Assert.IsFalse(SshProtocol.Protocol.IsAvailable(target.Object));
        }

        [Test]
        public void WhenTargetHasTrait_ThenIsAvailableReturnsTrue()
        {
            var target = new Mock<IProtocolTarget>();
            target
                .Setup(t => t.Traits)
                .Returns(new[] { LinuxTrait.Instance });

            Assert.IsTrue(SshProtocol.Protocol.IsAvailable(target.Object));
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToStringReturnsName()
        {
            Assert.AreEqual("SSH", SshProtocol.Protocol.ToString());
        }
    }
}
