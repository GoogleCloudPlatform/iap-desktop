//
// Copyright 2022 Google LLC
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
using System;
using System.Drawing;

namespace Google.Solutions.Mvvm.Test.Drawing
{
    [TestFixture]
    public class TestBadgeIcon
    {
        [Test]
        public void ForTextInitial_WhenTextIsNullOrEmpty_ThenForTextInitialThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => BadgeIcon.ForTextInitial(null!));
            Assert.Throws<ArgumentException>(() => BadgeIcon.ForTextInitial(string.Empty));
        }

        [Test]
        public void ForTextInitial_WhenTextValid_ThenIconHasBackColor()
        {
            using (var icon = BadgeIcon.ForTextInitial("Test"))
            {
                Assert.AreNotEqual(Color.White, icon.BackColor);
                Assert.AreNotEqual(Color.Black, icon.BackColor);
            }
        }
    }
}
