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

using Google.Solutions.Mvvm.Drawing;
using NUnit.Framework;

namespace Google.Solutions.Mvvm.Test.Drawing
{
    [TestFixture]
    public class TestColorExtensions
    {
        //---------------------------------------------------------------------
        // Conversion.
        //---------------------------------------------------------------------

        [Test]
        public void FromCOLORREF_WhenConvertedFromCOLORREF_ThenToCOLORREFReturnsSameValue(
            [Values(
                0x00000000,
                0x00FF0000,
                0x0000FF00,
                0x000000FF)]int colorref)
        {
            var color = ColorExtensions.FromCOLORREF((uint)colorref);
            Assert.AreEqual(colorref, color.ToCOLORREF());
        }

        [Test]
        public void FromCOLORREF_WhenHighByteIsNonZero_ThenFromCOLORREFSucceeds()
        {
            var color = ColorExtensions.FromCOLORREF(0xAABBCCDD);
            Assert.AreEqual(0xBBCCDD, color.ToCOLORREF());
        }
    }
}
