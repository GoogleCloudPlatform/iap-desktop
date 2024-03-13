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
using System.ComponentModel.DataAnnotations;

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
        public void WhenZero_ThenIsSingleFlagReturnsFalse()
        {
            var e = SampleFlags.Zero;
            Assert.IsFalse(e.IsSingleFlag());
            Assert.IsFalse(e.IsFlagCombination());
        }

        [Test]
        public void WhenOneFlagSet_ThenIsSingleFlagReturnsTrue()
        {
            var e = SampleFlags.Four;
            Assert.IsTrue(e.IsSingleFlag());
            Assert.IsFalse(e.IsFlagCombination());
        }

        [Test]
        public void WhenTwoFlagsSet_ThenIsSingleFlagReturnsFalse()
        {
            var e = SampleFlags.One | SampleFlags.Four;
            Assert.IsFalse(e.IsSingleFlag());
            Assert.IsTrue(e.IsFlagCombination());
        }

        //---------------------------------------------------------------------
        // IsValidFlagCombination
        //---------------------------------------------------------------------

        [Test]
        public void WhenAllFlagsClear_ThenIsValidFlagCombinationReturnsTrue()
        {
            Assert.IsTrue(SampleFlags.Zero.IsValidFlagCombination());
        }

        [Test]
        public void WhenOneFlagSet_ThenIsValidFlagCombinationReturnsTrue()
        {
            Assert.IsTrue(SampleFlags.One.IsValidFlagCombination());
        }

        [Test]
        public void WhenMultipleFlagsSet_ThenIsValidFlagCombinationReturnsTrue()
        {
            Assert.IsTrue((SampleFlags.One | SampleFlags.Four).IsValidFlagCombination());
        }

        [Test]
        public void WhenNonexistingFlagSet_ThenIsValidFlagCombinationReturnsFalse()
        {
            Assert.IsFalse((SampleFlags.One | (SampleFlags)16).IsValidFlagCombination());
        }

        //---------------------------------------------------------------------
        // GetAttribute
        //---------------------------------------------------------------------

        public enum SampleEnumWithAttributes
        {
            NoAttribute,

            [Display(Name = "With attribute")]
            WithAttribute
        }

        [Test]
        public void WhenValueHasAttribute_ThenGetAttributeReturnsValue()
        {
            var a = SampleEnumWithAttributes.WithAttribute.GetAttribute<DisplayAttribute>();
            Assert.IsNotNull(a);
            Assert.AreEqual("With attribute", a!.Name);
        }

        [Test]
        public void WhenValueHasNoAttribute_ThenGetAttributeReturnsNull()
        {
            var a = SampleEnumWithAttributes.NoAttribute.GetAttribute<DisplayAttribute>();
            Assert.IsNull(a);
        }
    }
}
