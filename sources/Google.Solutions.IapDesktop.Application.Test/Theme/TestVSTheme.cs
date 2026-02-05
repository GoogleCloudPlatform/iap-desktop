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

using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.Testing.Application.Test;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Theme
{
    [TestFixture]
    public class TestVSTheme : ApplicationFixtureBase
    {
        //---------------------------------------------------------------------
        // Load.
        //---------------------------------------------------------------------

        [Test]
        public void GetLightTheme()
        {
            var theme = VSTheme.GetLightTheme();

            Assert.That(theme, Is.Not.Null);
            Assert.That(theme.ColorPalette, Is.Not.Null);
        }

        [Test]
        public void GetDarkTheme()
        {
            var theme = VSTheme.GetDarkTheme();

            Assert.That(theme, Is.Not.Null);
            Assert.That(theme.ColorPalette, Is.Not.Null);
        }
    }
}
