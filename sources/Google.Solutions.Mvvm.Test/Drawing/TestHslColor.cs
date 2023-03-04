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

using Google.Solutions.Mvvm.Drawing;
using NUnit.Framework;

namespace Google.Solutions.Mvvm.Test.Drawing
{
    [TestFixture]
    public class TestHslColor
    {
        //---------------------------------------------------------------------
        // Conversion.
        //---------------------------------------------------------------------

        [Test]
        public void WhenConvertedFromRgb_ThenToRgbReturnsSameValue(
            [Values(
                0x00000000,
                0x00FF0000,
                0x0000FF00,
                0x000000FF)]int colorref)
        {
            var color = ColorExtensions.FromCOLORREF((uint)colorref);

            Assert.AreEqual(color, color.ToHsl().ToColor());
        }

        //---------------------------------------------------------------------
        // GetHashCode.
        //---------------------------------------------------------------------

        [Test]
        public void GetHashCodeEncodesHslValues()
        {
            Assert.AreEqual(0, new HslColor(0.0f, 0.0f, 0.0f).GetHashCode());
            Assert.AreEqual(0xFFFFFF, new HslColor(1.0f, 1.0f, 1.0f).GetHashCode());
            Assert.AreEqual(1651532, new HslColor(0.1f, 0.2f, 0.3f).GetHashCode());
        }

        //---------------------------------------------------------------------
        // Equals.
        //---------------------------------------------------------------------

        [Test]
        public void WhenValuesSame_ThenEqualsReturnTrue()
        {
            var c = new HslColor(1.0f, 1.0f, 1.0f);

            Assert.IsTrue(c.Equals(c));
            Assert.IsTrue(c.Equals(new HslColor(1.0f, 1.0f, 1.0f)));
            Assert.IsFalse(c.Equals(new HslColor(1.0f, 1.0f, 0.0f)));
            Assert.IsFalse(c.Equals(null));
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToStringReturnsHslValues()
        {
            var c = new HslColor(.1f, .2f, .3f);
            Assert.AreEqual("H=25, S=51, L=76", c.ToString());
        }
    }
}
