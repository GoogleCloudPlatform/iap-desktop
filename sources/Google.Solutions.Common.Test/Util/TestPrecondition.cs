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

using Google.Solutions.Common.Util;
using NUnit.Framework;
using System;

namespace Google.Solutions.Common.Test.Util
{
    [TestFixture]
    public class TestPrecondition
    {
        //---------------------------------------------------------------------
        // ExpectNotNull.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNull_ThenExpectNotNullThrowsException()
        {
            Assert.Throws<ArgumentNullException>(
                () => ((string)null).ExpectNotNull("test"));
        }

        [Test]
        public void WhenNotNull_ThenExpectNotNullReturnsValue()
        {
            Assert.AreEqual("value", "value".ExpectNotNull("test"));
            Assert.AreEqual(123, 123.ExpectNotNull("test"));
        }

        //---------------------------------------------------------------------
        // Expect.
        //---------------------------------------------------------------------

        [Test]
        public void WhenConditionFalse_ThenExpectThrowsException()
        {
            Assert.Throws<ArgumentException>(
                () => Precondition.Expect(false, "test"));
        }

        [Test]
        public void WhenConditionTrue_ThenExpectReturns()
        {
            Precondition.Expect(true, null);
            Precondition.Expect(true, null);
        }

        //---------------------------------------------------------------------
        // ExpectInRange.
        //---------------------------------------------------------------------

        [Test]
        public void WhenOutOfRange_ThenExpectInRangeThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => (1.1f).ExpectInRange(0f, 1f, "test"));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => (-.1f).ExpectInRange(0f, 1f, "test"));
        }

        [Test]
        public void WhenInRange_ThenExpectInRangeReturnsValue()
        {
            Assert.AreEqual(0f, (0f).ExpectInRange(-1f, 1f, null));
            Assert.AreEqual(1f, (1f).ExpectInRange(-1f, 1f, null));
            Assert.AreEqual(-1f, (-1f).ExpectInRange(-1f, 1f, null));
        }
    }
}
