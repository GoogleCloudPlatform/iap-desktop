//
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

namespace Google.Solutions.Common.Test.Util
{
    [TestFixture]
    public class TestEnumExtensions : CommonFixtureBase
    {
        //---------------------------------------------------------------------
        // IsSingleFlag
        //---------------------------------------------------------------------

        [Flags]
        public enum SampleFlags
        {
            Zero = 0,
            One = 1,
            Two = 2,
            Four = 4
        }

        [Test]
        public void IsSingleFlag_WhenZero_ThenIsSingleFlagReturnsFalse()
        {
            var e = SampleFlags.Zero;
            Assert.That(e.IsSingleFlag(), Is.False);
            Assert.That(e.IsFlagCombination(), Is.False);
        }

        [Test]
        public void IsSingleFlag_WhenOneFlagSet_ThenIsSingleFlagReturnsTrue()
        {
            var e = SampleFlags.Four;
            Assert.IsTrue(e.IsSingleFlag());
            Assert.That(e.IsFlagCombination(), Is.False);
        }

        [Test]
        public void IsSingleFlag_WhenTwoFlagsSet_ThenIsSingleFlagReturnsFalse()
        {
            var e = SampleFlags.One | SampleFlags.Four;
            Assert.That(e.IsSingleFlag(), Is.False);
            Assert.IsTrue(e.IsFlagCombination());
        }

        //---------------------------------------------------------------------
        // IsValidFlagCombination
        //---------------------------------------------------------------------

        [Test]
        public void IsValidFlagCombination_WhenAllFlagsClear_ThenIsValidFlagCombinationReturnsTrue()
        {
            Assert.IsTrue(SampleFlags.Zero.IsValidFlagCombination());
        }

        [Test]
        public void IsValidFlagCombination_WhenOneFlagSet_ThenIsValidFlagCombinationReturnsTrue()
        {
            Assert.IsTrue(SampleFlags.One.IsValidFlagCombination());
        }

        [Test]
        public void IsValidFlagCombination_WhenMultipleFlagsSet_ThenIsValidFlagCombinationReturnsTrue()
        {
            Assert.IsTrue((SampleFlags.One | SampleFlags.Four).IsValidFlagCombination());
        }

        [Test]
        public void IsValidFlagCombination_WhenNonexistingFlagSet_ThenIsValidFlagCombinationReturnsFalse()
        {
            Assert.That((SampleFlags.One | (SampleFlags)16).IsValidFlagCombination(), Is.False);
        }

        //---------------------------------------------------------------------
        // GetAttribute
        //---------------------------------------------------------------------

        public enum SampleEnumWithAttributes
        {
            NoAttribute,

            [System.ComponentModel.Description("With attribute")]
            WithAttribute
        }

        [Test]
        public void GetAttribute_WhenValueHasAttribute_ThenGetAttributeReturnsValue()
        {
            var a = SampleEnumWithAttributes.WithAttribute
                .GetAttribute<System.ComponentModel.DescriptionAttribute>();
            Assert.IsNotNull(a);
            Assert.That(a!.Description, Is.EqualTo("With attribute"));
        }

        [Test]
        public void GetAttribute_WhenValueHasNoAttribute_ThenGetAttributeReturnsNull()
        {
            var a = SampleEnumWithAttributes.NoAttribute
                .GetAttribute<System.ComponentModel.DescriptionAttribute>();
            Assert.IsNull(a);
        }
    }
}
