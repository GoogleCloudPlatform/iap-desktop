﻿//
// Copyright 2020 Google LLC
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

using Google.Solutions.Common.Util;
using NUnit.Framework;
using System;
using System.Reflection;

namespace Google.Solutions.Common.Test.Util
{
    [TestFixture]
    public class TestEnumExtensions : FixtureBase
    {
        [Flags]
        public enum SomeEnum
        {
            Zero = 0,
            One = 1,
            Two = 2,
            Four = 4
        }

        [Test]
        public void WhenZero_ThenIsSingleFlagReturnsFalse()
        {
            var e = SomeEnum.Zero;
            Assert.IsFalse(e.IsSingleFlag());
            Assert.IsFalse(e.IsFlagCombination());
        }

        [Test]
        public void WhenOneFlagSet_ThenIsSingleFlagReturnsTrue()
        {
            var e = SomeEnum.Four;
            Assert.IsTrue(e.IsSingleFlag());
            Assert.IsFalse(e.IsFlagCombination());
        }

        [Test]
        public void WhenTwoFlagsSet_ThenIsSingleFlagReturnsFalse()
        {
            var e = SomeEnum.One | SomeEnum.Four;
            Assert.IsFalse(e.IsSingleFlag());
            Assert.IsTrue(e.IsFlagCombination());
        }
    }
}
