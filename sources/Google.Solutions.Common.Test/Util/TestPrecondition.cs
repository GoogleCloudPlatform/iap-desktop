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

using Google.Solutions.Common.Util;
using NUnit.Framework;
using System;

namespace Google.Solutions.Common.Test.Util
{
    [TestFixture]
    public class TestPrecondition
    {
        //---------------------------------------------------------------------
        // ThrowIfNot.
        //---------------------------------------------------------------------

        [Test]
        public void WhenConditionFalse_ThenThrowIfNotThrowsException()
        {
            Assert.Throws<ArgumentException>(
                () => "".ThrowIfNot(false, "test"));
        }

        [Test]
        public void WhenConditionTrue_ThenThrowIfNotReturnsValue()
        {
            Assert.AreEqual(0, 0.ThrowIfNot(true, null));
            Assert.AreEqual("test", "test".ThrowIfNot(true, null));
        }

        //---------------------------------------------------------------------
        // ThrowIfOutOfRange.
        //---------------------------------------------------------------------

        [Test]
        public void WhenOutOfRange_ThenThrowIfOutOfRangeThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => (1.1f).ThrowIfOutOfRange(0f, 1f, "test"));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => (-.1f).ThrowIfOutOfRange(0f, 1f, "test"));
        }

        [Test]
        public void WhenInRange_ThenThrowIfOutOfRangeReturnsValue()
        {
            Assert.AreEqual(0f, (0f).ThrowIfOutOfRange(-1f, 1f, null));
            Assert.AreEqual(1f, (1f).ThrowIfOutOfRange(-1f, 1f, null));
            Assert.AreEqual(-1f, (-1f).ThrowIfOutOfRange(-1f, 1f, null));
        }
    }
}
