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

using Google.Solutions.Mvvm.Drawing;
using NUnit.Framework;
using System.Drawing;
using System.Threading;

namespace Google.Solutions.Mvvm.Test.Drawing
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestFontExtensions
    {
        [Test]
        public void IsMonospaced_WhenFontIsVariableSpaced_ThenIsMonospacedIsFalce()
        {
            using (var font = new Font(FontFamily.GenericSansSerif, 10))
            {
                Assert.IsFalse(font.IsMonospaced());
            }
        }

        [Test]
        public void IsMonospaced_WhenFontIsMonospaced_ThenIsMonospacedIsFalce()
        {
            using (var font = new Font(FontFamily.GenericMonospace, 10))
            {
                Assert.IsTrue(font.IsMonospaced());
            }
        }
    }
}
