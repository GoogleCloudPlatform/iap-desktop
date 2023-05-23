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

using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.Testing.Apis;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Core.Test.ClientModel.Protocol
{
    [TestFixture]
    public class TestAppProtocolComand
        : EquatableFixtureBase<AppProtocol.Command, AppProtocol.Command>
    {
        protected override AppProtocol.Command CreateInstance()
        {
            return new AppProtocol.Command("cmd.exe", "args");
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToStringReturnsExecutableAndArguments()
        {
            Assert.AreEqual(
                "cmd.exe",
                new AppProtocol.Command("cmd.exe", null).ToString());
            Assert.AreEqual(
                "cmd.exe args",
                new AppProtocol.Command("cmd.exe", "args").ToString());
        }

        //---------------------------------------------------------------------
        // Equals.
        //---------------------------------------------------------------------

        [Test]
        public void WhenOtherHasDifferentExecutable_ThenEqualsReturnsFalse()
        {
            var command1 = new AppProtocol.Command("cmd1.exe", "args");
            var command2 = new AppProtocol.Command("cmd2.exe", "args");

            Assert.IsFalse(command1.Equals(command2));
            Assert.IsTrue(command1 != command2);
            Assert.AreNotEqual(command1.GetHashCode(), command2.GetHashCode());
        }

        [Test]
        public void WhenOtherHasDifferentArguments_ThenEqualsReturnsFalse()
        {
            var command1 = new AppProtocol.Command("cmd.exe", "args");
            var command2 = new AppProtocol.Command("cmd.exe", null);

            Assert.IsFalse(command1.Equals(command2));
            Assert.IsTrue(command1 != command2);
            Assert.AreNotEqual(command1.GetHashCode(), command2.GetHashCode());
        }
    }
}
